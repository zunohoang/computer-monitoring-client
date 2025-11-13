using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Networks;
using System.Threading.Tasks;


namespace ComputerMonitoringClient.Services
{
    public class ContestService
    {
        private readonly ILogger<ContestService> _logger;

        public ContestService()
        {
            _logger = LoggerProvider.CreateLogger<ContestService>();
        }

        /// <summary>
        /// Join a contest room with the provided access code and student information
        /// </summary>
        /// <param name="request">Join room request containing accessCode, sbd, ipAddress, and location</param>
        /// <returns>JoinRoomResponse containing attempt details and authentication token</returns>
        public async Task<JoinRoomResponse?> JoinContestRoomAsync(JoinRoomRequest request)
        {
            try
            {
                DeviceDetailDto deviceDetails = await DeviceService.Instance.GetDeviceDetailAsync();
                request.ipAddress = deviceDetails.IPAddress;
                request.location = deviceDetails.Location;
                _logger?.LogInformation("Attempting to join contest room - SBD: {SBD}, AccessCode: {AccessCode}",
                    request.sbd, request.accessCode);

                var response = await ApiClient.Instance.PostAsync<JoinRoomResponse>("Room/join", request);

                if (response != null)
                {
                    _logger?.LogInformation("Successfully joined contest room - AttemptId: {AttemptId}, RoomId: {RoomId}, Status: {Status}",
                        response.attemptId, response.roomId, response.status);
                }
                else
                {
                    _logger?.LogWarning("Join contest room returned null response");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error joining contest room - SBD: {SBD}, AccessCode: {AccessCode}",
                    request.sbd, request.accessCode);
                throw;
            }
        }
    }
}