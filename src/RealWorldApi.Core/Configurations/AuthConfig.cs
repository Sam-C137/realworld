using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RealWorldApi.Core.Extensions;

namespace RealWorldApi.Core.Configurations;

public static class AuthConfig
{
    public static void AddAuthConfig(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                    ValidAudience            = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                opts.Events = new JwtBearerEvents
                {
                    OnTokenValidated = SessionValidationExtensions.ValidateSessionAsync
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    }
}