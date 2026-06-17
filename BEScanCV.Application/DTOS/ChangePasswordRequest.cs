using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [JsonPropertyName("confirmNewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}