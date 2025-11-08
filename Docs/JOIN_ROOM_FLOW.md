# Join Room Status Flow

## Tổng quan

Hệ thống xử lý 3 trạng thái khi tham gia phòng thi:

### 1. **PENDING** (Chờ phê duyệt)

- **Mô tả**: Yêu cầu tham gia phòng thi đang chờ giám thị phê duyệt
- **UI**: Hiển thị `PendingForm` với spinner loading
- **Hành động**:
  - Form tự động kiểm tra trạng thái mỗi 3 giây
  - Hiển thị thông tin: Họ tên, SBD, Phòng thi
  - Cho phép người dùng hủy và quay lại
- **Chuyển đến**:
  - `APPROVED` → Chuyển sang MonitoringForm
  - `REJECTED` → Hiển thị lỗi và đóng form

### 2. **APPROVED** (Đã phê duyệt)

- **Mô tả**: Yêu cầu đã được chấp nhận
- **UI**: Hiển thị notification thành công
- **Hành động**:
  - Lưu session với AuthenticationService
  - Chuyển sang MonitoringForm
  - Hiển thị thông tin: Họ tên, SBD, Phòng thi

### 3. **REJECTED** (Bị từ chối)

- **Mô tả**: Yêu cầu bị giám thị từ chối
- **UI**: Hiển thị modal lỗi
- **Hành động**:
  - Hiển thị lý do từ chối (nếu có)
  - Button login được enable lại để thử lại

## Luồng xử lý

```
User Input (SBD + Access Code)
        ↓
  Validate Input
        ↓
Get IP & Location
        ↓
Call JoinContestRoomAsync
        ↓
    Response?
    ↙   ↓   ↘
PENDING APPROVED REJECTED
    ↓       ↓         ↓
PendingForm → MonitoringForm
    ↓                 ↓
Auto-check (3s)    Show Error
    ↓               Enable Login
APPROVED/REJECTED
    ↓
MonitoringForm/Error
```

## Code Files

### JoinRoomResponse.cs

- `IsPending` property - Check nếu status = "pending"
- `IsApproved` property - Check nếu status = "approved"
- `IsRejected` property - Check nếu status = "rejected"

### LoginForm.cs

- Validate input fields
- Get device info (IP, location)
- Call API `JoinContestRoomAsync`
- Route theo status:
  - Pending → Show PendingForm
  - Approved → Show MonitoringForm
  - Rejected → Show error modal

### PendingForm.cs

- Display waiting UI với spinner
- Timer auto-check status mỗi 3 giây
- Handle status changes:
  - Approved → Navigate to MonitoringForm
  - Rejected → Show error và close
- Allow user cancel và quay lại LoginForm

## API Request/Response

### Request (JoinRoomRequest)

```json
{
  "accessCode": "string",
  "sbd": 12345,
  "ipAddress": "192.168.1.1",
  "location": "Ho Chi Minh City, Vietnam"
}
```

### Response (JoinRoomResponse)

```json
{
  "attemptId": 1,
  "roomId": 10,
  "contestId": 5,
  "sbd": 12345,
  "fullName": "Nguyen Van A",
  "status": "pending|approved|rejected",
  "message": "Waiting for approval...",
  "token": "auth-token-here"
}
```

## UI Components

### PendingForm Features

- ⏳ Loading spinner animation
- 👤 Hiển thị thông tin user (Họ tên, SBD, Phòng)
- 🔄 Auto-refresh status (3 giây/lần)
- ❌ Nút Hủy để quay lại
- 📱 Responsive layout

### Status Messages

- **Pending**: "Yêu cầu của bạn đang chờ giám thị phê duyệt"
- **Approved**: "Bạn đã được chấp nhận vào phòng thi!"
- **Rejected**: "Yêu cầu của bạn đã bị từ chối! [reason]"

## Error Handling

- Network errors → Show connection error
- Null response → Show server error
- Unknown status → Show invalid status error
- All errors re-enable login button
