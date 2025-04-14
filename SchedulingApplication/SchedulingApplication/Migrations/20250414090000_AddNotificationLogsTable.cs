using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SchedulingApplication.Migrations
{
    public partial class AddNotificationLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 检查表是否已存在
            migrationBuilder.Sql(
                @"SELECT name 
                  FROM sqlite_master 
                  WHERE type='table' AND name='NotificationLogs';",
                true);

            // 创建通知日志表
            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SentTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientPhone = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");
        }
    }
} 