
### Gigling Racing API
- Hackuton rules: https://app.notion.com/p/GIGATHON-1-Gigaverse-Hackathon-June-26-84a28bd838c682ad91578176c12adf9d
- MCP server for DOCS: gigaverse-glhfers https://docs.gigaverse.io/~gitbook/mcp
- Gigling Racing Overview: https://docs.gigaverse.io/gigling-racing/gigling-racing-overview
- Gigling Racing Builder Guide: https://app.notion.com/p/Gigling-Racing-Builder-Guide-33128bd838c683a594da81cbf51e87fb

### OBS
- OBS Browser Source docs: https://obsproject.com/kb/browser-source
- OBS Developer Guide: https://obsproject.com/kb/developer-guide
- obs-websocket repository: https://github.com/obsproject/obs-websocket
- obs-websocket protocol spec: https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md
- obs-browser source/plugin repository: https://github.com/obsproject/obs-browser

### .NET / Microsoft
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy
- .NET lifecycle page: https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core
- WPF docs: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- ASP.NET Core Minimal API tutorial: https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api
- Minimal API quick reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- WebView2 docs, optional: https://learn.microsoft.com/en-us/microsoft-edge/webview2/landing/
- Microsoft Learn MCP Server: https://learn.microsoft.com/en-us/training/support/mcp
- Microsoft Learn MCP endpoint reference: https://learn.microsoft.com/en-us/training/support/mcp-developer-reference
- MCP server for DOCS: microsoftLearn https://learn.microsoft.com/api/mcp 

## Useful Libraries/Packages

### Required / Recommended
```bash
dotnet add package H.NotifyIcon.Wpf
```
Use `H.NotifyIcon.Wpf` only if you implement tray behavior and native notifications. Keep it out of the first commit if Codex struggles with XAML.

### Optional OBS control
```bash
dotnet add package obs-websocket-dotnet
```
Use this only after Browser Source overlay works. It can control OBS through WebSocket but is not needed for MVP.

Alternative modern OBS WebSocket client to evaluate later:
- `ObsWebSocket.Core`: https://github.com/Agash/ObsWebSocket

### Open-Source Projects to Study

These are not exact Gigling Racing competitors. They are adjacent examples for OBS overlays, browser-source workflows, lower thirds, and stream control patterns.

| Repository | Why useful |
|---|---|
| https://github.com/obsproject/obs-browser | Understand OBS Browser Source implementation and CEF assumptions |
| https://github.com/obsproject/obs-websocket | Official OBS remote-control protocol and request/event model |
| https://github.com/BarRaider/obs-websocket-dotnet | C# client for OBS WebSocket if you add OBS scene/source control |
| https://github.com/spenibus/obs-overlay-html-js | Older but useful HTML/JS overlay pattern |
| https://github.com/filiphanes/websocket-overlays | WebSocket-controlled HTML overlays for OBS/XSplit/CasparCG |
| https://github.com/filiphanes/web-overlays | Remote-controlled web overlays; useful architecture reference |
| https://github.com/hennedo/lowerThirdsHTML | Lower-third Browser Source overlay example |
| https://github.com/rse/lowerthird | Plain HTML/CSS/JS lower-third overlay for OBS |
| https://github.com/noeal-dac/Animated-Lower-Thirds | Control panel + animated lower thirds; useful operator-panel idea |
| https://github.com/geerlingguy/obs-task-list-overlay | Local Node/HTML overlay added to OBS as Browser Source |
| https://github.com/detekoi/static-browser-overlays | Static Browser Source overlay examples |
| https://github.com/hperrin/stream-overlay | Transparent always-on-top browser overlay app; useful if adding desktop overlay mode |

Cloned some repositories to local folder for Codex to study code patterns and architecture. Focused on C# projects and HTML/JS overlay examples: 
C:\Users\sgame\Documents\gigavers-web-hack-app\3rd-partys

