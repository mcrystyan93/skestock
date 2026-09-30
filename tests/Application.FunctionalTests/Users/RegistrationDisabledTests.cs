using System.Net;
using System.Net.Http.Json;

namespace skestock.Application.FunctionalTests.Users;

public class RegistrationDisabledTests
{
    [TestCase("/api/Users/register")]
    [TestCase("/api/users/REGISTER")]
    public async Task Register_Anonymous_Returns404(string path)
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(path, new { email = "new@local", password = "Testing1234!" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Login_StillReachable()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "nobody@local", password = "Wrong1234!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
