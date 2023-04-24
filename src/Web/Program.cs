using Whirl1.Data;
using Whirl1.Services;
using Whirl1.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IPasswordService, PasswordService>();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication("WhirlAuth")
    .AddCookie("WhirlAuth", options =>
    {
        options.Cookie.Name = "WhirlCookie";
        //options.EventsType = typeof(CustomCookieAuthenticationEvents);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsRootOrAdmin", policy =>
                                policy.RequireClaim("Role", "Root", "Admin")
                                      .RequireClaim("ShowModControls", "True"));

    options.AddPolicy("IsManager", policy =>
                                policy.RequireClaim("Role", "Root", "Admin", "Moderator"));

    options.AddPolicy("IsValidUser",
        policy => policy.AddRequirements(new IsValidUserRequirement()));

    options.AddPolicy("IsHigherAuthority",
        policy => policy.AddRequirements(new IsHigherAuthorityRequirement()));                       

    options.AddPolicy("EditComment",
        policy => policy.AddRequirements(new EditCommentRequirement()));

    options.AddPolicy("DeleteComment",
        policy => policy.AddRequirements(new DeleteCommentRequirement()));

    options.AddPolicy("DeletePost",
        policy => policy.AddRequirements(new DeletePostRequirement()));

});

builder.Services.AddTransient<IAuthorizationHandler, IsValidUserHandler>();
builder.Services.AddTransient<IAuthorizationHandler, IsHigherAuthorityHandler>();
builder.Services.AddTransient<IAuthorizationHandler, EditCommentHandler>();
builder.Services.AddTransient<IAuthorizationHandler, DeleteCommentHandler>();
builder.Services.AddTransient<IAuthorizationHandler, DeletePostHandler>();

//builder.Services.AddTransient<CustomCookieAuthenticationEvents>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Forum}/{action=Index}/{id?}");

app.Run();