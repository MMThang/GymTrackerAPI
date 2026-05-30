using Dapper;
using GymTracker.DTOs.SetDTOs;
using GymTracker.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace GymTracker.Repositories
{
    public class SetRepository : ISet
    {
        private readonly IConfiguration _configuration;

        public SetRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public async Task<bool> createSet(Guid exerciseId, SetDTO setDTO, IDbTransaction? transaction = null)
        {
            // Validate: if isBodyWeight is false, weight must have a value
            if (!setDTO.isBodyWeight && setDTO.weight == null)
            {
                throw new ArgumentException("Weight must have a value when isBodyWeight is false.");
            }

            if (transaction != null)
            {
                await transaction.Connection.ExecuteAsync(
                    "INSERT INTO \"Sets\" (\"SetId\", \"ExerciseId\", \"Weight\", \"Reps\", \"IsBodyWeight\") VALUES (gen_random_uuid(), @ExerciseId, @Weight, @Reps, @IsBodyWeight)",
                    new { ExerciseId = exerciseId, Weight = setDTO.weight, Reps = setDTO.reps, IsBodyWeight = setDTO.isBodyWeight },
                    transaction
                );
                Console.WriteLine($"[TRANSACTION] Set created successfully for ExerciseId: {exerciseId}");
            }
            else
            {
                await using var connection = GetConnection();
                await connection.ExecuteAsync(
                    "INSERT INTO \"Sets\" (\"SetId\", \"ExerciseId\", \"Weight\", \"Reps\", \"IsBodyWeight\") VALUES (gen_random_uuid(), @ExerciseId, @Weight, @Reps, @IsBodyWeight)",
                    new { ExerciseId = exerciseId, Weight = setDTO.weight, Reps = setDTO.reps, IsBodyWeight = setDTO.isBodyWeight }
                );
            }
            return true;
        }

        public async Task<bool> updateSet(Guid setId, SetDTO setDTO, IDbTransaction? transaction = null)
        {
            // Validate: if isBodyWeight is false, weight must have a value
            if (!setDTO.isBodyWeight && setDTO.weight == null)
            {
                throw new ArgumentException("Weight must have a value when isBodyWeight is false.");
            }

            if (transaction != null)
            {
                await transaction.Connection.ExecuteAsync(
                    "UPDATE \"Sets\" SET \"Weight\" = @Weight, \"Reps\" = @Reps, \"IsBodyWeight\" = @IsBodyWeight WHERE \"SetId\" = @SetId",
                    new { Weight = setDTO.weight, Reps = setDTO.reps, IsBodyWeight = setDTO.isBodyWeight, SetId = setId },
                    transaction
                );
            }
            else
            {
                await using var connection = GetConnection();
                await connection.ExecuteAsync(
                    "UPDATE \"Sets\" SET \"Weight\" = @Weight, \"Reps\" = @Reps, \"IsBodyWeight\" = @IsBodyWeight WHERE \"SetId\" = @SetId",
                    new { Weight = setDTO.weight, Reps = setDTO.reps, IsBodyWeight = setDTO.isBodyWeight, SetId = setId }
                );
            }
            return true;
        }

        public async Task<bool> deleteSet(Guid setId, IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                await transaction.Connection.ExecuteAsync(
                    "DELETE FROM \"Sets\" WHERE \"SetId\" = @SetId",
                    new { SetId = setId },
                    transaction
                );
            }
            else
            {
                await using var connection = GetConnection();
                await connection.ExecuteAsync(
                    "DELETE FROM \"Sets\" WHERE \"SetId\" = @SetId",
                    new { SetId = setId }
                );
            }
            return true;
        }
    }
}
