# HTMX vs Blazor Server for real-time quiz applications

For a Kahoot-style multiplayer quiz with ~100s concurrent users on a hobby budget, **HTMX with SignalR** offers the simpler path with lower hosting costs and better resilience, while **Blazor Server** provides faster initial development for .NET-native teams but requires more careful infrastructure planning. Both are technically viable—the choice hinges on whether you prefer hypermedia simplicity or full C# component architecture.

The fundamental architectural difference is stark: Blazor Server maintains **~250-300KB of circuit state per connection** on the server (holding your entire component tree), while HTMX is **stateless by default** with only lightweight SSE or WebSocket connections. This difference cascades through every aspect of hosting, scaling, and resilience decisions.

---

## How each architecture handles the quiz flow

The critical quiz pattern—host broadcasts question, participants see it simultaneously, answers submitted, leaderboard updates live—reveals fundamental differences in how each technology operates.

### Blazor Server's circuit-based approach

Blazor Server runs over SignalR with every UI interaction round-tripping to the server. When a host clicks "Next Question," the server updates a shared singleton service that triggers `StateHasChanged()` across all participant circuits:

```csharp
// Singleton game state service
public class QuizStateService
{
    public event Action<QuizState>? OnStateChanged;
    public void BroadcastState(QuizState state) => OnStateChanged?.Invoke(state);
}

// In each participant's component
protected override void OnInitialized()
{
    QuizStateService.OnStateChanged += HandleStateChange;
}

private void HandleStateChange(QuizState state)
{
    CurrentState = state;
    InvokeAsync(StateHasChanged);  // Must call on UI thread
}
```

The server holds **full component trees** for every connected user. A button click travels to the server, C# code executes, a render diff is computed, and only the DOM changes are sent back. This feels magical but creates tight coupling between connection state and user experience.

### HTMX's hypermedia approach with SignalR

HTMX can integrate with SignalR via the community **htmx-signalr extension**, combining HTML-over-the-wire simplicity with .NET's real-time infrastructure:

```html
<div hx-ext="signalr" signalr-connect="/quizHub">
  <!-- Automatically swaps when server sends "NewQuestion" event -->
  <div signalr-subscribe="NewQuestion" id="question-area">
    Waiting for host to start...
  </div>
  
  <div signalr-subscribe="LeaderboardUpdate" id="leaderboard"></div>
  
  <!-- Form sends to SignalR hub method -->
  <form signalr-send="SubmitAnswer">
    <button name="answer" value="A">Option A</button>
    <button name="answer" value="B">Option B</button>
  </form>
</div>
```

The server sends **complete HTML fragments** that replace DOM elements directly. No component state lives on the server between requests—each interaction is independent. The hub broadcasts to SignalR groups just like in Blazor, but what travels the wire is ready-to-render HTML.

---

## Real-time latency and connection behavior

### Blazor Server demands consistent low latency

Every UI interaction in Blazor Server requires a SignalR round-trip. Microsoft's guidance: at **~100ms latency** users notice lag; at **~200ms** the app can "break" as requests desynchronize. Geographic distance compounds across interactions because all UI logic executes server-side.

Reconnection behavior is built-in but can frustrate users. When connections drop, Blazor shows "Attempting to reconnect..." for up to **3 minutes** (configurable) while preserving circuit state. If the circuit times out, users must reload and **lose all unsaved state**. .NET 9 improved this with exponential backoff, but the fundamental limitation remains.

### HTMX's stateless model handles disruption gracefully

HTMX latency equals network round-trip plus server processing—comparable to any web request. The key difference: a failed request doesn't break application state. SSE connections reconnect automatically per browser spec, and the htmx-signalr extension adds exponential backoff.

Server restarts affect both approaches, but HTMX users simply reconnect and continue. Blazor Server users lose their circuits and must reload. For a quiz app where the authoritative game state lives in the database anyway, HTMX's statelessness means **connection issues don't derail the experience**.

---

## Memory footprint and what it means for hosting

### Blazor Server's per-circuit overhead

Microsoft documents **~250-300KB baseline** per circuit for minimal applications. Real-world complexity increases this substantially. For 100 concurrent users, budget **25-50MB** for framework overhead alone, plus your application's per-user state.

The calculation complicates with disconnected circuits: Blazor retains them for 3 minutes hoping users reconnect. During network hiccups, you might have both active and "zombie" circuits consuming memory simultaneously.

```csharp
// Configure circuit retention
builder.Services.AddServerSideBlazor(options =>
{
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
});
```

### HTMX's minimal server-side footprint

HTMX itself is **~14KB client-side** with zero server-side framework state. Server memory depends purely on your application logic and connection handling. SSE connections are lightweight HTTP streams; SignalR connections share the same infrastructure as Blazor but without component tree overhead.

For a hobby project, this difference matters: a **$6/month DigitalOcean droplet** (1 vCPU, 1GB RAM) comfortably handles HTMX with hundreds of users. Blazor Server needs **2GB+ RAM** to breathe comfortably, pushing you toward $12-18/month territory.

---

## Scaling beyond a single server

### Blazor Server requires careful orchestration

Scaling Blazor Server across multiple instances demands either **sticky sessions** (so circuits reconnect to their original server) or **Azure SignalR Service** (which offloads connection management entirely).

Azure SignalR Service pricing creates a sharp decision point:
- **Free tier**: 20 concurrent connections, 20,000 messages/day—testing only
- **Standard tier**: ~$41/month per unit (1,000 connections each)

For a hobby project with occasional spikes to hundreds of users, Azure SignalR Service might cost more than your entire hosting otherwise would. The self-hosted alternative (Redis backplane) requires managing sticky sessions and introduces a Redis dependency.

### HTMX scales like any stateless web app

Standard load balancing works immediately for HTMX HTTP requests—no special configuration. SSE connections benefit from sticky sessions but don't strictly require them if all servers can broadcast events (via Redis pub/sub or similar).

The practical implication: you can start on a single cheap VPS and scale horizontally with standard infrastructure patterns when needed, without framework-specific complexity.

---

## Developer experience and learning curve

### HTMX: learn in a day, debug in the network tab

Developers consistently report picking up HTMX in **a few hours to a day**. The mental model is HTML plus attributes—no component lifecycle, no state management framework, no build pipeline. Everything you need to debug appears in the browser's Network tab as clear HTTP requests and HTML responses.

The tradeoff: no IntelliSense for HTMX attributes in Visual Studio (VS Code has extensions), and the declarative nature means less static analysis catching errors. Complex UI state requires explicit management through hidden inputs or headers.

```html
<!-- What you write is what happens -->
<button hx-post="/submit-answer" 
        hx-vals='{"questionId": 5, "answer": "B"}'
        hx-target="#result"
        hx-swap="innerHTML">
    Option B
</button>
```

### Blazor Server: familiar .NET patterns with hidden complexity

.NET developers feel at home immediately—it's C#, components, dependency injection. Initial productivity is excellent; one team built a working Kahoot-clone in **8 hours**. The razor syntax and component model mirror patterns from other frameworks.

The hidden complexity emerges later: circuit lifecycle behaviors, `InvokeAsync(StateHasChanged)` requirements for cross-thread updates, memory leak patterns from event subscription mismanagement, and the "Attempting to reconnect" dance during deployments. Community reports describe a **steeper debugging curve** when SignalR connections misbehave.

---

## Practical quiz implementation patterns

### Blazor Server component architecture

A natural pattern emerges with shared services and event-driven component updates:

```
[Browser] ←SignalR→ [Blazor Server]
                         ↓
                [QuizGameManager] (Singleton)
                    ├── ConcurrentDictionary<gameCode, GameRoom>
                    ├── Timer service for question timing
                    └── Broadcasts via events
```

Each component subscribes to the game manager's events and calls `StateHasChanged()` when state changes. The challenge: ensuring all subscriptions are properly disposed to avoid memory leaks, and handling the case where circuits disconnect mid-question.

### HTMX + SignalR organization

The pattern separates concerns cleanly:

```
/Pages
  /Quiz
    Host.cshtml            # Host view with HTMX
    Join.cshtml            # Participant join
    _Question.cshtml       # Partial for question display
    _Leaderboard.cshtml    # Partial for leaderboard
/Hubs
  QuizHub.cs               # SignalR hub
/Services
  GameService.cs           # Game state (database-backed)
  RazorPartialRenderer.cs  # Render partials to strings
```

The hub renders partials to HTML strings and broadcasts them:

```csharp
public async Task BroadcastQuestion(string gameId, int questionIndex)
{
    var html = await _razorRenderer.RenderPartialAsync("_Question", question);
    await Clients.Group(gameId).SendAsync("NewQuestion", html);
}
```

---

## Known pitfalls specific to quiz applications

### Blazor Server gotchas

**Deployment disrupts active quizzes**: There's no clean way to deploy updates without disconnecting all users. Every deployment potentially interrupts games in progress, losing circuit state.

**Mobile browsers aggressively close connections**: Background tabs on mobile trigger WebSocket disconnections, causing participants to see reconnection modals when they switch apps briefly.

**Timer synchronization**: Keep authoritative time server-side and broadcast remaining seconds—don't rely on client timers staying in sync.

### HTMX gotchas

**SSE browser connection limit**: Browsers allow only **~6 concurrent SSE connections per domain**. If your quiz has multiple SSE streams (questions, leaderboard, timer), this constraint matters.

**CDN reliability**: If HTMX doesn't load from CDN, your entire UI fails silently. Bundle locally for production.

**State lives in the DOM**: Without explicit server-side persistence, refreshing the page loses client context. Quiz progress should be database-backed regardless of framework.

---

## The hybrid approach worth considering

The **HTMX + SignalR hybrid** deserves serious consideration: use HTMX for form submissions and standard interactions, SignalR exclusively for real-time push events. This gives you:

- Stateless HTTP for answers and navigation
- Real-time broadcasts for question distribution and leaderboard updates
- Clear separation between user actions and server pushes
- Standard web architecture patterns

The htmx-signalr extension (55+ GitHub stars, actively maintained) makes this integration straightforward. You get SignalR's .NET ecosystem advantages without Blazor Server's circuit overhead.

---

## Hosting recommendations for hobby budgets

| Approach | Minimum Viable | Comfortable | Monthly Cost |
|----------|---------------|-------------|--------------|
| **HTMX + SSE/SignalR** | 1 vCPU, 512MB | 1 vCPU, 1GB | **$5-10** |
| **Blazor Server** | 1 vCPU, 2GB | 2 vCPU, 4GB | **$12-24** |
| **Blazor + Azure SignalR** | App Service B1 | Standard S1 | **$55-115** |

For ~100 concurrent users on either approach, a basic VPS handles the load. The difference: HTMX has more headroom on cheaper instances, while Blazor Server's memory consumption scales with connections.

**DigitalOcean App Platform**, **Render**, and **Railway** all support both approaches in the $7-20/month range. Azure App Service works but requires enabling WebSockets in configuration for Blazor Server.

---

## Recommendation for your quiz project

For a **hobby/side-project** building a Kahoot-like quiz:

**Choose HTMX + SignalR if** you want simpler hosting, better resilience to connection issues, and a more debuggable architecture. The htmx-signalr extension handles real-time broadcasts while keeping your server stateless. You'll spend more time thinking about UI but less time debugging circuit lifecycle issues.

**Choose Blazor Server if** your team is deeply invested in .NET component patterns and you value development speed over operational simplicity. You'll prototype faster but need to plan for connection resilience, deployment strategies, and potentially Azure SignalR Service costs as you scale.

The community has built working quiz implementations with both approaches. For Blazor, [Quizor](https://github.com/rasmuskl/Quizor) demonstrates the exact Kahoot-like pattern. For HTMX, combine [htmx-signalr](https://github.com/Renerick/htmx-signalr) with Khalid Abuhakmeh's [ASP.NET Core patterns](https://github.com/khalidabuhakmeh/htmx-aspnetcore).

At hobby scale with hundreds of users, technical capability isn't the differentiator—both work. The question is which failure modes and operational characteristics fit your tolerance for complexity.
