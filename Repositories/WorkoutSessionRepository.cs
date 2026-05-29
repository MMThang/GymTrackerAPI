using Dapper;
using GymTracker.Data;
using GymTracker.DTOs.ExerciseDTOs;
using GymTracker.DTOs.SetDTOs;
using GymTracker.DTOs.WorkoutSessionDTOs;
using GymTracker.Entities;
using GymTracker.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace GymTracker.Repositories
{
    public class WorkoutSessionRepository : IWorkoutSession
    {
        private readonly AppDBContext _context;
        private readonly IExercise _exercise;
        private readonly ISet _set;
        private readonly IConfiguration _configuration;

        public WorkoutSessionRepository(AppDBContext context, IExercise exercise, ISet set, IConfiguration configuration)
        {
            _context = context;
            this._exercise = exercise;
            this._set = set;
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("WebApiDatabase"));
        }

        public async Task<bool> createWorkoutSession(CreateWorkoutSessionDTO workoutSessionDTO)
        {
            await using var connection = GetConnection();
            var existingUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @UserId", new { UserId = workoutSessionDTO.userId });
            if (existingUser != null)
            {
                if (!DateOnly.TryParseExact(workoutSessionDTO.createDate, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out var parsedCreateDate))
                {
                    throw new Exception("Invalid createDate format. Use yyyy/mm/dd");
                }

                // Check if user already has a workout session with the same create date
                var existingWorkoutSession = await connection.QueryFirstOrDefaultAsync<WorkoutSession>(
                    "SELECT * FROM \"WorkoutSessions\" WHERE \"UserId\" = @UserId AND \"CreateDate\" = @CreateDate",
                    new { UserId = workoutSessionDTO.userId, CreateDate = parsedCreateDate }
                );

                if (existingWorkoutSession != null)
                {
                    throw new Exception("User already has a workout session on this date");
                }

                var workoutSession = new WorkoutSession
                {
                    UserId = workoutSessionDTO.userId,
                    WorkoutSessionName = workoutSessionDTO.workoutSessionName,
                    Notes = workoutSessionDTO.note,
                    CreateDate = parsedCreateDate,
                    Exercises = workoutSessionDTO.exercises.Select(e => new Exercise
                    {
                        ExerciseName = e.exerciseName,
                        Sets = e.sets.Select(s => new Set
                        {
                            Reps = s.reps,
                            Weight = s.weight
                        }).ToList()
                    }).ToList()
                };
                await _context.WorkoutSessions.AddAsync(workoutSession);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                throw new Exception("User not found");
            }
        }

        public async Task<Guid?> getWorkoutSessionUserId(Guid workoutSessionId)
        {
            await using var connection = GetConnection();
            var sql = "SELECT \"UserId\" FROM \"WorkoutSessions\" WHERE \"WorkoutSessionId\" = @WorkoutSessionId";
            var userId = await connection.QueryFirstOrDefaultAsync<Guid?>(sql, new { WorkoutSessionId = workoutSessionId });
            return userId;
        }

        public async Task<List<WorkoutSessionSummaryDTO>> getWorkoutSessionsList(Guid userId)
        {
            await using var connection = GetConnection();

            var userExists = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @UserId", new { UserId = userId });
            if (userExists == null)
            {
                throw new Exception("User not found");
            }

            var sql = @"
                SELECT 
                    ws.""WorkoutSessionId"",
                    ws.""WorkoutSessionName"",
                    COALESCE(COUNT(DISTINCT e.""ExerciseId""), 0) AS ""NumberOfExercises"",
                    COALESCE(COUNT(s.""SetId""), 0) AS ""NumberOfSets"",
                    ws.""CreateDate""
                FROM ""WorkoutSessions"" ws
                LEFT JOIN ""Exercises"" e ON ws.""WorkoutSessionId"" = e.""WorkoutSessionId""
                LEFT JOIN ""Sets"" s ON e.""ExerciseId"" = s.""ExerciseId""
                WHERE ws.""UserId"" = @UserId
                GROUP BY ws.""WorkoutSessionId"", ws.""WorkoutSessionName"", ws.""CreateDate""
                ORDER BY ws.""CreateDate"" DESC
                LIMIT 7";

            var result = await connection.QueryAsync<WorkoutSessionSummaryDTO>(sql, new { UserId = userId });
            return result.ToList();
        }

        public async Task<WorkoutSessionDetailDTO> getWorkoutSession(Guid workoutSessionId)
        {
            await using var connection = GetConnection();

            var workoutSessionSql = @"
                SELECT ""WorkoutSessionId"", ""WorkoutSessionName"", ""Notes""
                FROM ""WorkoutSessions""
                WHERE ""WorkoutSessionId"" = @WorkoutSessionId";

            var workoutSession = await connection.QueryFirstOrDefaultAsync<dynamic>(workoutSessionSql, new { WorkoutSessionId = workoutSessionId });

            if (workoutSession == null)
            {
                throw new Exception("WorkoutSession not found");
            }

            var exercisesSql = @"
                SELECT ""ExerciseId"", ""ExerciseName"", ""WorkoutSessionId""
                FROM ""Exercises""
                WHERE ""WorkoutSessionId"" = @WorkoutSessionId";

            var exercises = await connection.QueryAsync<dynamic>(exercisesSql, new { WorkoutSessionId = workoutSessionId });

            var setsSql = @"
                SELECT s.""SetId"", s.""Weight"", s.""IsBodyWeight"", s.""Reps"", s.""ExerciseId""
                FROM ""Sets"" s
                WHERE s.""ExerciseId"" IN (SELECT ""ExerciseId"" FROM ""Exercises"" WHERE ""WorkoutSessionId"" = @WorkoutSessionId)";

            var sets = await connection.QueryAsync<dynamic>(setsSql, new { WorkoutSessionId = workoutSessionId });

            var exerciseDetails = new List<ExerciseDetailDTO>();
            foreach (var exercise in exercises)
            {
                var exerciseSets = sets.Where(s => s.ExerciseId == exercise.ExerciseId).ToList();
                exerciseDetails.Add(new ExerciseDetailDTO
                {
                    ExerciseId = exercise.ExerciseId,
                    ExerciseName = exercise.ExerciseName,
                    Sets = exerciseSets.Select(set => new SetDetailDTO
                    {
                        SetId = set.SetId,
                        Weight = set.Weight,
                        IsBodyWeight = set.IsBodyWeight,
                        Reps = set.Reps
                    }).ToList()
                });
            }

            var workoutSessionDetailDTO = new WorkoutSessionDetailDTO
            {
                WorkoutSessionId = workoutSession.WorkoutSessionId,
                WorkoutSessionName = workoutSession.WorkoutSessionName,
                Notes = workoutSession.Notes,
                Exercises = exerciseDetails
            };

            return workoutSessionDetailDTO;
        }

        public async Task<List<WorkoutSessionCalendarDTO>> getWorkoutSessionsByMonthYear(Guid userId, int month, int year)
        {
            await using var connection = GetConnection();

            // Verify user exists
            var userExists = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @UserId", new { UserId = userId });
            if (userExists == null)
            {
                throw new Exception("User not found");
            }

            // Get today's date for validation
            var today = DateTime.UtcNow.Date;
            var requestedDate = new DateTime(year, month, 1);
            var oneYearAgo = today.AddYears(-1);
            // Validate month is not in the future
            if ((requestedDate > today) || (requestedDate < oneYearAgo))
            {
                throw new Exception("Cannot request months");
            }

            // Query workout sessions for the given month/year
            var sql = @"
                SELECT 
                    ws.""WorkoutSessionId"",
                    ws.""CreateDate"",
                    ws.""Notes""
                FROM ""WorkoutSessions"" ws
                WHERE ws.""UserId"" = @UserId
                  AND EXTRACT(MONTH FROM ws.""CreateDate"") = @Month
                  AND EXTRACT(YEAR FROM ws.""CreateDate"") = @Year
                ORDER BY ws.""CreateDate"" ASC";

            var workoutSessions = await connection.QueryAsync<dynamic>(sql, new { UserId = userId, Month = month, Year = year });

            // Get today's date to limit current month
            var isCurrentMonth = today.Year == year && today.Month == month;

            // Generate all days in the month (or up to today if it's the current month)
            var maxDay = isCurrentMonth ? today.Day : DateTime.DaysInMonth(year, month);
            var calendarDays = new List<WorkoutSessionCalendarDTO>();

            for (int day = 1; day <= maxDay; day++)
            {
                var date = new DateOnly(year, month, day);
                var workoutForDay = workoutSessions.FirstOrDefault(ws => DateOnly.FromDateTime(ws.CreateDate) == date);

                calendarDays.Add(new WorkoutSessionCalendarDTO
                {
                    Date = date,
                    HasWorkoutSession = workoutForDay != null,
                    WorkoutSessionId = workoutForDay?.WorkoutSessionId,
                    HasNote = workoutForDay != null && !string.IsNullOrEmpty(workoutForDay?.Notes)
                });
            }

            return calendarDays;
        }

        public async Task<bool> updateWorkoutSession(UpdateWorkoutSessionDTO updateDTO)
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var workoutSession = await connection.QueryFirstOrDefaultAsync<WorkoutSession>(
                    "SELECT * FROM \"WorkoutSessions\" WHERE \"WorkoutSessionId\" = @WorkoutSessionId",
                    new { WorkoutSessionId = updateDTO.workoutSessionId },
                    transaction
                );

                if (workoutSession == null)
                    throw new Exception("Workout session not found");

                // Update session
                if (
                    workoutSession.WorkoutSessionName != updateDTO.workoutSessionName ||
                    workoutSession.Notes != updateDTO.note
                )
                {
                    await connection.ExecuteAsync(
                        "UPDATE \"WorkoutSessions\" SET \"WorkoutSessionName\" = @WorkoutSessionName, \"Notes\" = @Notes WHERE \"WorkoutSessionId\" = @WorkoutSessionId",
                        new { WorkoutSessionName = updateDTO.workoutSessionName, Notes = updateDTO.note, WorkoutSessionId = updateDTO.workoutSessionId },
                        transaction
                    );
                }

                // Fetch existing exercises and sets
                var existingExercises = await connection.QueryAsync<Exercise>(
                    "SELECT * FROM \"Exercises\" WHERE \"WorkoutSessionId\" = @WorkoutSessionId",
                    new { WorkoutSessionId = updateDTO.workoutSessionId },
                    transaction
                );

                var exercisesDictionary = existingExercises.ToDictionary(e => e.ExerciseId);
                var existingSets = await connection.QueryAsync<Set>(
                    @"SELECT s.* FROM ""Sets"" s
                    INNER JOIN ""Exercises"" e ON s.""ExerciseId"" = e.""ExerciseId""
                    WHERE e.""WorkoutSessionId"" = @WorkoutSessionId",
                    new { WorkoutSessionId = updateDTO.workoutSessionId },
                    transaction
                );

                var setsDictionary = existingSets.ToDictionary(s => s.SetId);
                var processedExerciseIds = new HashSet<Guid>();

                // Process incoming exercises and sets - delegate to respective repositories with transaction
                foreach (var exerciseDTO in updateDTO.exercises)
                {
                    if (exerciseDTO.exerciseId.HasValue)
                    {
                        var exerciseId = exerciseDTO.exerciseId.Value;

                        if (exercisesDictionary.TryGetValue(exerciseId, out var existingExercise) == false)
                            throw new Exception($"Exercise with ID {exerciseId} not found");

                        processedExerciseIds.Add(exerciseId);

                        // Delegate to ExerciseRepository with transaction
                        if (existingExercise.ExerciseName != exerciseDTO.exerciseName)
                        {
                            await _exercise.updateExercise(exerciseId, exerciseDTO.exerciseName, transaction);
                        }

                        var exerciseSets = setsDictionary.Where(s => s.Value.ExerciseId == exerciseId).Select(s => s.Value).ToList();
                        var processedSetIds = new HashSet<Guid>();

                        // Process sets - delegate to SetRepository with transaction
                        foreach (var setDTO in exerciseDTO.sets)
                        {
                            if (setDTO.setId.HasValue)
                            {
                                var setId = setDTO.setId.Value;
                                if (setsDictionary.TryGetValue(setId, out var set) == false)
                                    throw new Exception($"Set with ID {setId} not found");

                                processedSetIds.Add(setId);
                                if (setDTO.weight != set.Weight || setDTO.reps != set.Reps || setDTO.isBodyWeight != set.IsBodyWeight)
                                {
                                    SetDTO updatedSetDTO = new SetDTO
                                    {
                                        weight = setDTO.weight,
                                        reps = setDTO.reps,
                                        isBodyWeight = setDTO.isBodyWeight
                                    };
                                    await _set.updateSet(setId, updatedSetDTO, transaction);
                                }
                            }
                            else
                            {
                                // Create new set via SetRepository with transaction
                                SetDTO createSetDTO = new SetDTO
                                {
                                    weight = setDTO.weight,
                                    reps = setDTO.reps,
                                    isBodyWeight = setDTO.isBodyWeight
                                };
                                await _set.createSet(exerciseId, createSetDTO, transaction);
                            }
                        }

                        // Delete removed sets
                        var setsToDelete = exerciseSets.Where(s => !processedSetIds.Contains(s.SetId)).ToList();
                        foreach (var setToDelete in setsToDelete)
                        {
                            await _set.deleteSet(setToDelete.SetId, transaction);
                        }
                    }
                    else
                    {

                        // Create new exercise via ExerciseRepository with transaction
                        Console.WriteLine($"[TRANSACTION] About to create new exercise: {exerciseDTO.exerciseName}");
                        var exerciseId = await _exercise.createExercise(updateDTO.workoutSessionId, exerciseDTO.exerciseName, transaction);
                        Console.WriteLine($"[TRANSACTION] New exercise created with ID: {exerciseId}");
                        var existingExercise = connection.QueryFirstOrDefault<Exercise>(
                            "SELECT * FROM \"Exercises\" WHERE \"ExerciseId\" = @ExerciseId",
                            new { ExerciseId = exerciseId },
                            transaction
                        );
                        Console.WriteLine($"[TRANSACTION] Exercise exists in DB: {existingExercise != null}");
                        foreach (var setDTO in exerciseDTO.sets)
                        {
                            Console.WriteLine($"[TRANSACTION] About to create set for ExerciseId: {exerciseId}");
                            await _set.createSet(exerciseId, setDTO, transaction);
                            Console.WriteLine($"[TRANSACTION] Set created successfully");
                        }
                    }
                }

                // Delete removed exercises
                var exercisesToDelete = exercisesDictionary.Where(e => !processedExerciseIds.Contains(e.Key)).Select(e => e.Value).ToList();
                foreach (var exerciseToDelete in exercisesToDelete)
                {
                    await _exercise.deleteExercise(exerciseToDelete.ExerciseId, transaction);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> deleteWorkoutSession(Guid workoutSessionId)
        {
            throw new NotImplementedException();
        }


    }
}
