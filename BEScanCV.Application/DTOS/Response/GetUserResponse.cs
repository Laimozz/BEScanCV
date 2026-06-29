using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public class GetUserResponse
{
    public UserItemDto User { get; set; } = null!;

}
