using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // pastikan user sudah login
public class SmartAssistantController : ControllerBase
{
    private readonly SmartAssistantServiceGenZAngry _smartService;

    public SmartAssistantController(SmartAssistantServiceGenZAngry smartService)
    {
        _smartService = smartService;
    }

    [HttpGet("saran")]
    public async Task<ActionResult<SaranKeuanganDto>> GetSaran()
    {
        // Ambil id user dari token JWT
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        int idUser = int.Parse(userIdClaim.Value);

        var saran = await _smartService.GenerateSmartSaranAngryGenZAsync(idUser);
        return Ok(saran);
    }
}
