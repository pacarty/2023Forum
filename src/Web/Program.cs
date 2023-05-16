using Forum.Data;
using Forum.Services;
using Forum.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IPasswordService, PasswordService>();

builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<DataContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("LocalConnection")));
}
else
{
    builder.Services.AddDbContext<DataContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("ProductionConnection")));
}

builder.Services.AddAuthentication("WhirlAuth")
    .AddCookie("WhirlAuth", options =>
    {
        options.Cookie.Name = "WhirlCookie";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsManager",
        policy => policy.AddRequirements(new IsManagerRequirement()));

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

    options.AddPolicy("EditDeleteCategoryTopic",
        policy => policy.AddRequirements(new EditDeleteCategoryTopicRequirement()));

    options.AddPolicy("EditUserRole",
        policy => policy.AddRequirements(new EditUserRoleRequirement()));

});

builder.Services.AddTransient<IAuthorizationHandler, IsManagerHandler>();
builder.Services.AddTransient<IAuthorizationHandler, IsValidUserHandler>();
builder.Services.AddTransient<IAuthorizationHandler, IsHigherAuthorityHandler>();
builder.Services.AddTransient<IAuthorizationHandler, EditCommentHandler>();
builder.Services.AddTransient<IAuthorizationHandler, DeleteCommentHandler>();
builder.Services.AddTransient<IAuthorizationHandler, DeletePostHandler>();
builder.Services.AddTransient<IAuthorizationHandler, EditDeleteCategoryTopicHandler>();
builder.Services.AddTransient<IAuthorizationHandler, EditUserRoleHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Rather than having the default HomeController, the ForumController acts as the default route of the website
app.MapControllerRoute("default", "{controller=Forum}/{action=Index}/{id?}");

app.Run();