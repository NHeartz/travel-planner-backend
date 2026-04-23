var builder = WebApplication.CreateBuilder(args);

// 1. เพิ่ม Services ที่จำเป็น
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // จำเป็นสำหรับการยิง HTTP Request ไปหา Gemini API

// 2. ตั้งค่า CORS เพื่ออนุญาตให้ React (Vite) เข้าถึง API ได้
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173", 
                  "https://travelpanner.vercel.app"
              ) 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. เปิดใช้งาน CORS (ต้องวางก่อน UseAuthorization และ MapControllers)
app.UseCors("AllowFrontend"); 

app.UseAuthorization();
app.MapControllers();

app.Run();
