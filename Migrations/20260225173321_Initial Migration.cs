using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamsyaSetuV2.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artifact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultSLADays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DepartmentEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ward", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Ward_WardId",
                        column: x => x.WardId,
                        principalTable: "Ward",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssueRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ArtifactId = table.Column<int>(type: "int", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    LocationText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Submitted"),
                    Priority = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Medium"),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBreached = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EscalationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    WardId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueRequest_Artifact_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueRequest_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueRequest_Users_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueRequest_Ward_WardId",
                        column: x => x.WardId,
                        principalTable: "Ward",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CitizenFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenFeedback_IssueRequest_IssueRequestId",
                        column: x => x.IssueRequestId,
                        principalTable: "IssueRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackRequest_IssueRequest_IssueRequestId",
                        column: x => x.IssueRequestId,
                        principalTable: "IssueRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackRequest_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenFeedback_IssueRequestId",
                table: "CitizenFeedback",
                column: "IssueRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueRequest_ArtifactId",
                table: "IssueRequest",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueRequest_AssignedToId",
                table: "IssueRequest",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueRequest_CitizenId",
                table: "IssueRequest",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueRequest_WardId",
                table: "IssueRequest",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackRequest_ChangedByUserId",
                table: "TrackRequest",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackRequest_IssueRequestId",
                table: "TrackRequest",
                column: "IssueRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WardId",
                table: "Users",
                column: "WardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitizenFeedback");

            migrationBuilder.DropTable(
                name: "TrackRequest");

            migrationBuilder.DropTable(
                name: "IssueRequest");

            migrationBuilder.DropTable(
                name: "Artifact");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Ward");
        }
    }
}
