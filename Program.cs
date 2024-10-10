using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enuns;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

#region Builder
var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration.GetSection("Jwt").ToString();


builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();


builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT aqui"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores
string GerarTokenJwt(Administrador administrador)
{
    if (string.IsNullOrEmpty(key)) return string.Empty;

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>() {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil)
    };
    var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials
        );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/login", ([FromBody] LoginDto loginDto, IAdministradorServico administradorServico) =>
{
    var adm = administradorServico.Login(loginDto);
    if (adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdiministradoLogadoModelView
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
    {
        return Results.Unauthorized();
    }
}).AllowAnonymous().WithTags("Adminstradores");

app.MapPost("/Adminstradores", ([FromBody] AdministradorDto administradorDto, IAdministradorServico administradorServico) =>
{
    var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };

    if (string.IsNullOrEmpty(administradorDto.Email))
        validacao.Mensagens.Add("Email não pode ser vazio");
    if (string.IsNullOrEmpty(administradorDto.Senha))
        validacao.Mensagens.Add("Senha não pode ser vazia");

    if (validacao.Mensagens.Any())
    {
        return Results.BadRequest(validacao.Mensagens);
    }

    var administrador = new Administrador
    {
        Perfil = administradorDto.Perfil.ToString(),
        Email = administradorDto.Email,
        Senha = administradorDto.Senha,
    };

    administradorServico.Incluir(administrador);

    return Results.Created($"/administrador/{administrador.Id}", administrador);
}).RequireAuthorization().WithTags("Adminstradores");

app.MapGet("/Adminstradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    var administradores = administradorServico.Todos(pagina);
    var adms = administradores.Select(x => new AdministradorModelView
    {
        Email = x.Email,
        Perfil = x.Perfil,
        Id = x.Id,
    });

    return Results.Ok(adms);
}).RequireAuthorization().WithTags("Adminstradores");

app.MapGet("/Adminstradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.BuscaPorId(id);

    if (administrador == null)
        return Results.NotFound();

    var adm = new AdministradorModelView
    {
        Id = administrador.Id,
        Perfil = administrador.Perfil,
        Email = administrador.Email,
    };

    return Results.Ok(adm);
}).RequireAuthorization().WithTags("Adminstradores");
#endregion

#region Veiculos
ErrosDeValidacao ValidaDto(VeiculoDto veiculoDto)
{
    var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };

    if (string.IsNullOrEmpty(veiculoDto.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");
    if (string.IsNullOrEmpty(veiculoDto.Marca))
        validacao.Mensagens.Add("A marca não ficar em branco");
    if (veiculoDto.Ano < 1950)
        validacao.Mensagens.Add("Veiculo muito antigo, aceito somente anos superioas a 1950");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDto veiculoDto, IVeiculoServico veiculoServico) =>
{
    var validacao = ValidaDto(veiculoDto);

    if (validacao.Mensagens.Any())
    {
        return Results.BadRequest(validacao.Mensagens);
    }

    var veiculo = new Veiculo
    {
        Nome = veiculoDto.Nome,
        Marca = veiculoDto.Marca,
        Ano = veiculoDto.Ano,
    };

    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
}).RequireAuthorization().WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
    return Results.Ok(veiculoServico.Todos(pagina));
}).RequireAuthorization().WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo == null)
        return Results.NotFound();

    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDto veiculoDto, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo == null)
        return Results.NotFound();

    var validacao = ValidaDto(veiculoDto);

    if (validacao.Mensagens.Any())
    {
        return Results.BadRequest(validacao.Mensagens);
    }

    veiculo.Nome = veiculoDto.Nome;
    veiculo.Marca = veiculoDto.Marca;
    veiculo.Ano = veiculoDto.Ano;

    veiculoServico.Atuaizar(veiculo);
    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo == null)
        return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
}).RequireAuthorization().WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion



