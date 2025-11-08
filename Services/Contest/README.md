# Contest Service - Join Room

## Overview

The `ContestService` provides functionality to join a contest room using the API endpoint `/api/Room/join`.

## Usage Example

```csharp
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Services.DeviceService;

// Create the service instance
var contestService = new ContestService();

// Get device IP and location
var deviceService = new DeviceService();
var ipAddress = deviceService.GetLocalIPAddress();
var location = deviceService.GetLocation();

// Create the join room request
var request = new JoinRoomRequest
{
    accessCode = "YOUR_ACCESS_CODE",
    sbd = 12345, // Student ID number
    ipAddress = ipAddress,
    location = location
};

try
{
    // Call the join room method
    var response = await contestService.JoinContestRoomAsync(request);

    if (response != null)
    {
        Console.WriteLine($"Successfully joined room!");
        Console.WriteLine($"Attempt ID: {response.attemptId}");
        Console.WriteLine($"Room ID: {response.roomId}");
        Console.WriteLine($"Contest ID: {response.contestId}");
        Console.WriteLine($"Full Name: {response.fullName}");
        Console.WriteLine($"Status: {response.status}");
        Console.WriteLine($"Message: {response.message}");
        Console.WriteLine($"Token: {response.token}");

        // Store the token for future API calls
        // You can save this token to use in subsequent requests
    }
    else
    {
        Console.WriteLine("Failed to join room - no response received");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error joining room: {ex.Message}");
}
```

## Request Model (JoinRoomRequest)

| Property   | Type   | Description                      |
| ---------- | ------ | -------------------------------- |
| accessCode | string | Access code for the contest room |
| sbd        | int    | Student ID number (Số báo danh)  |
| ipAddress  | string | IP address of the client machine |
| location   | string | Geographic location information  |

## Response Model (JoinRoomResponse)

| Property  | Type   | Description                              |
| --------- | ------ | ---------------------------------------- |
| attemptId | int    | Unique ID for this attempt               |
| roomId    | int    | ID of the room joined                    |
| contestId | int    | ID of the contest                        |
| sbd       | int    | Student ID number                        |
| fullName  | string | Full name of the student                 |
| status    | string | Status of the join request               |
| message   | string | Response message                         |
| token     | string | Authentication token for future requests |

## Error Handling

The service method will throw an exception if:

- Network connection fails
- API returns a 400 Bad Request or other error
- Request data is invalid

Always wrap the call in a try-catch block to handle potential errors gracefully.

## HTTP Response Codes

- **200 OK**: Successfully joined the room
- **400 Bad Request**: Invalid request data or access code
