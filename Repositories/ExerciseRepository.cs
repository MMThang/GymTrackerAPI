using Dapper;
using GymTracker.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace GymTracker.Repositories
{
    public class ExerciseRepository : IExercise
    {
        private readonly IConfiguration _configuration;

        public ExerciseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public async Task<Guid> createExercise(Guid workoutSessionId, string exerciseName, IDbTransaction? transaction = null)
        {
            Guid exerciseId;

            if (transaction != null)
            {
                exerciseId = await transaction.Connection.QueryFirstOrDefaultAsync<Guid>(
                    "INSERT INTO \"Exercises\" (\"ExerciseId\", \"WorkoutSessionId\", \"ExerciseName\") VALUES (gen_random_uuid(), @WorkoutSessionId, @ExerciseName) RETURNING \"ExerciseId\"",
                    new { WorkoutSessionId = workoutSessionId, ExerciseName = exerciseName },
                    transaction
                );
                Console.WriteLine($"[TRANSACTION] Exercise created with ID: {exerciseId}");
            }
            else
            {
                await using var connection = GetConnection();
                exerciseId = await connection.QueryFirstOrDefaultAsync<Guid>(
                    "INSERT INTO \"Exercises\" (\"ExerciseId\", \"WorkoutSessionId\", \"ExerciseName\") VALUES (gen_random_uuid(), @WorkoutSessionId, @ExerciseName) RETURNING \"ExerciseId\"",
                    new { WorkoutSessionId = workoutSessionId, ExerciseName = exerciseName }
                );
            }
            return exerciseId;
        }

        public async Task<bool> updateExercise(Guid exerciseId, string exerciseName, IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                await transaction.Connection.ExecuteAsync(
                    "UPDATE \"Exercises\" SET \"ExerciseName\" = @ExerciseName WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseName = exerciseName, ExerciseId = exerciseId },
                    transaction
                );
            }
            else
            {
                await using var connection = GetConnection();
                await connection.ExecuteAsync(
                    "UPDATE \"Exercises\" SET \"ExerciseName\" = @ExerciseName WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseName = exerciseName, ExerciseId = exerciseId }
                );
            }
            return true;
        }

        public async Task<bool> deleteExercise(Guid exerciseId, IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                // Delete sets first
                await transaction.Connection.ExecuteAsync(
                    "DELETE FROM \"Sets\" WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseId = exerciseId },
                    transaction
                );
                // Delete exercise
                await transaction.Connection.ExecuteAsync(
                    "DELETE FROM \"Exercises\" WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseId = exerciseId },
                    transaction
                );
            }
            else
            {
                await using var connection = GetConnection();
                // Delete sets first
                await connection.ExecuteAsync(
                    "DELETE FROM \"Sets\" WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseId = exerciseId }
                );
                // Delete exercise
                await connection.ExecuteAsync(
                    "DELETE FROM \"Exercises\" WHERE \"ExerciseId\" = @ExerciseId",
                    new { ExerciseId = exerciseId }
                );
            }
            return true;
        }
    }
}
