using minimal_api.Dominio.DTOs;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Olá Pessoal!");

app.MapPost("/login", (LoginDto loginDto) =>
{
    if(loginDto.Email == "adm@teste.com" && loginDto.Senha == "123456")
    {
        return Results.Ok("Login com sucesso");
    }
    else
    {
        return Results.Unauthorized();
    }
});

app.Run();



