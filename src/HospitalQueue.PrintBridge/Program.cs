var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration.GetValue<int>("Port", 9100);
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(port));

builder.Services.AddCors(options =>
{
    // The kiosk's own browser (a different origin/port: the app server) calls
    // this local service, so CORS must allow it. Safe here because Kestrel
    // only listens on localhost — nothing outside this PC can reach it.
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/print", (PrintRequest request, IConfiguration config, ILogger<Program> logger) =>
{
    if (!OperatingSystem.IsWindows())
    {
        return Results.Problem("خدمة الطباعة هذه مصممة للعمل على Windows فقط.", statusCode: 500);
    }

    var printerName = config["PrinterName"];
    if (string.IsNullOrWhiteSpace(printerName) || printerName == "REPLACE_WITH_EXACT_WINDOWS_PRINTER_NAME")
    {
        return Results.Problem(
            "لم يتم ضبط اسم الطابعة بعد. افتح appsettings.json وضع اسم الطابعة كما يظهر تماماً في إعدادات Windows.",
            statusCode: 500);
    }

    try
    {
        var widthDots = config.GetValue("PaperWidthDots", 384);
        using var bitmap = HospitalQueue.PrintBridge.TicketImageRenderer.Render(
            request.Number, request.ServiceName, request.IssuedAt, widthDots);
        var bytes = HospitalQueue.PrintBridge.EscPosBuilder.BuildTicketReceipt(bitmap);
        HospitalQueue.PrintBridge.RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ticket print failed");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.Run();

record PrintRequest(string Number, string ServiceName, string IssuedAt);
