using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixParticipantResultsRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_Participants_ParticipantId1",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_ParticipantId1",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "ParticipantId1",
                table: "Results");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParticipantId1",
                table: "Results",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_ParticipantId1",
                table: "Results",
                column: "ParticipantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Participants_ParticipantId1",
                table: "Results",
                column: "ParticipantId1",
                principalTable: "Participants",
                principalColumn: "ParticipantId");
        }
    }
}
