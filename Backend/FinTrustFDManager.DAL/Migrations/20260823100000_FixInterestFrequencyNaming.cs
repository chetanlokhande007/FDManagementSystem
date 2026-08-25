using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixInterestFrequencyNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename "Yearly" to "Annually" in the InterestFrequencies table
            // to match the backend validation which expects "ANNUALLY".
            // Also update any existing FDInterest records that reference "Yearly".
            migrationBuilder.Sql(
                "UPDATE \"InterestFrequencies\" SET \"FrequencyName\" = 'Annually' WHERE \"Id\" = 4 AND \"FrequencyName\" = 'Yearly'");

            migrationBuilder.Sql(
                "UPDATE \"FDInterests\" SET \"InterestFrequency\" = 'Annually' WHERE \"InterestFrequency\" = 'Yearly'");

            migrationBuilder.Sql(
                "UPDATE \"FDInterests\" SET \"CompoundingFrequency\" = 'Annually' WHERE \"CompoundingFrequency\" = 'Yearly'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"InterestFrequencies\" SET \"FrequencyName\" = 'Yearly' WHERE \"Id\" = 4 AND \"FrequencyName\" = 'Annually'");

            migrationBuilder.Sql(
                "UPDATE \"FDInterests\" SET \"InterestFrequency\" = 'Yearly' WHERE \"InterestFrequency\" = 'Annually'");

            migrationBuilder.Sql(
                "UPDATE \"FDInterests\" SET \"CompoundingFrequency\" = 'Yearly' WHERE \"CompoundingFrequency\" = 'Annually'");
        }
    }
}
