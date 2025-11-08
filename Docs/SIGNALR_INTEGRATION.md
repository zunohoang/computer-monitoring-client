# SignalR Real-time Monitoring Integration

## Tổng quan

Hệ thống đã được cập nhật để sử dụng **SignalR** thay vì polling để theo dõi trạng thái real-time của attempt.

## Luồng hoạt động

### 1. **Login và nhận Token**

```
User Login → API Call JoinRoom → Response {
    token: "jwt-token",
    attemptId: 123,
    status: "pending"
}
```

### 2. **Kết nối SignalR Hub**

```
PendingForm → MonitoringHubClient.ConnectAsync(token, attemptId)
    ↓
Connect to: /hubs/monitor
    ↓
Authentication: Bearer {token}
    ↓
Join Group: "Attempt_{attemptId}"
```

### 3. **Lắng nghe Status Updates**

```
SignalR Hub → Broadcast "StatusUpdated" or "AttemptStatusUpdated"
    ↓
MonitoringHubClient.OnStatusUpdated event fired
    ↓
PendingForm.HandleStatusUpdate(status)
    ↓
┌─────────────────┐
│ status?         │
├─────────────────┤
│ "approved"  → MonitoringForm
│ "rejected"  → Error Modal + Close
│ "pending"   → Continue waiting
└─────────────────┘
```

## Code Components

### MonitoringHubClient.cs

**SignalR client service** để kết nối và lắng nghe hub

#### Constructor

```csharp
var hubClient = new MonitoringHubClient();
```

#### Methods

- `ConnectAsync(token, attemptId)` - Kết nối tới hub với authentication
- `DisconnectAsync()` - Ngắt kết nối
- `IsConnected` - Kiểm tra trạng thái kết nối

#### Events

- `OnStatusUpdated` - Status của attempt thay đổi (approved/rejected/pending)
- `OnConnected` - Kết nối thành công
- `OnDisconnected` - Mất kết nối
- `OnError` - Có lỗi xảy ra

#### Hub Methods Listening

- `StatusUpdated(string status)` - Server gửi status update
- `AttemptStatusUpdated(int attemptId, string status)` - Update cho attempt cụ thể
- `MonitoringUpdate(string message)` - General monitoring updates
- `Error(string errorMessage)` - Lỗi từ server

### PendingForm.cs

**UI form** hiển thị trạng thái pending và xử lý updates

#### Changes từ Polling → SignalR

**TRƯỚC (Polling):**

```csharp
// Check status every 3 seconds
Timer checkStatusTimer = new Timer { Interval = 3000 };
checkStatusTimer.Tick += async (s, e) => {
    var response = await contestService.JoinContestRoomAsync(request);
    // Check status...
};
```

**SAU (SignalR):**

```csharp
// Connect once and listen for updates
hubClient.OnStatusUpdated += HandleStatusUpdate;
await hubClient.ConnectAsync(token, attemptId);
```

#### Benefits

- ✅ **Real-time** - Ngay lập tức nhận update, không delay 3 giây
- ✅ **Efficient** - Không cần gọi API liên tục
- ✅ **Scalable** - Server push thay vì client poll
- ✅ **Auto-reconnect** - Tự động kết nối lại khi mất kết nối

### LoginForm.cs

**Không thay đổi logic**, chỉ cập nhật cách tạo PendingForm:

```csharp
// Old: Pass all parameters for polling
PendingForm pendingForm = new PendingForm(response, roomCode, sbd, ipAddress, location);

// New: Only need response and roomCode for SignalR
PendingForm pendingForm = new PendingForm(response, roomCode);
```

## SignalR Hub Configuration

### Server-side Hub URL

```
{BASE_URL}/hubs/monitor
```

Ví dụ:

- Development: `http://localhost:5045/hubs/monitor`
- Production: `https://your-domain.com/hubs/monitor`

### Authentication

Hub sử dụng **JWT Bearer Token** authentication:

```csharp
options.AccessTokenProvider = () => Task.FromResult<string?>(token);
```

### Group Management

Client tự động join vào group theo attemptId:

```csharp
await hubConnection.InvokeAsync("JoinAttemptGroup", attemptId);
```

## Event Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Server (SignalR Hub)                      │
│                                                              │
│  Attempt Status Changed                                      │
│         ↓                                                    │
│  Broadcast to Group "Attempt_{attemptId}"                    │
│         ↓                                                    │
│  Send: StatusUpdated("approved")                             │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ↓ (Real-time push)
┌──────────────────────────────────────────────────────────────┐
│                Client (MonitoringHubClient)                   │
│                                                              │
│  Receive: StatusUpdated("approved")                          │
│         ↓                                                    │
│  Fire Event: OnStatusUpdated.Invoke("approved")              │
│         ↓                                                    │
│  PendingForm.HandleStatusUpdate("approved")                  │
│         ↓                                                    │
│  Navigate to MonitoringForm                                  │
└──────────────────────────────────────────────────────────────┘
```

## Error Handling

### Connection Errors

```csharp
hubClient.OnError += (error) => {
    lblMessage.Text = $"Lỗi kết nối: {error}\nĐang thử kết nối lại...";
};
```

### Disconnection Handling

```csharp
hubClient.OnDisconnected += (ex) => {
    // Log error
    // SignalR auto-reconnects with .WithAutomaticReconnect()
};
```

### Reconnection

SignalR tự động reconnect với exponential backoff:

```csharp
.WithAutomaticReconnect()
```

## Dependencies Added

### NuGet Package

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
```

### Using Statements

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
```

## Testing

### Test Scenarios

1. **Pending → Approved**

   - Login → Pending form shows
   - Admin approves → Auto navigate to MonitoringForm

2. **Pending → Rejected**

   - Login → Pending form shows
   - Admin rejects → Error modal + back to login

3. **Connection Lost**

   - Disconnect network
   - SignalR auto-reconnects
   - Continue receiving updates

4. **User Cancel**
   - Click "Hủy" button
   - Disconnect from hub
   - Return to login form

## Performance Comparison

| Feature       | Polling (Old)                   | SignalR (New)   |
| ------------- | ------------------------------- | --------------- |
| Update Delay  | 0-3 seconds                     | < 100ms         |
| Network Calls | Every 3s                        | Only on change  |
| Server Load   | High (N clients × polling rate) | Low (push only) |
| Scalability   | Limited                         | High            |
| Real-time     | No                              | Yes             |
| Reconnection  | Manual                          | Automatic       |

## Security

- ✅ JWT Token authentication
- ✅ Token passed in connection options (not in URL)
- ✅ Group-based access (only see own attempt)
- ✅ Server validates token and attempt ownership
