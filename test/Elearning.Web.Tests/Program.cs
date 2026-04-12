using Microsoft.AspNetCore.Builder;
using Elearning;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("Elearning.Web.csproj");
await builder.RunAbpModuleAsync<ElearningWebTestModule>(applicationName: "Elearning.Web" );

public partial class Program
{
}
