using System.Data;

namespace GymTracker.Interfaces
{
    public interface IExercise
    {
        Task<Guid> createExercise(Guid workoutSessionId, string exerciseName, IDbTransaction? transaction = null);
        Task<bool> updateExercise(Guid exerciseId, string exerciseName, IDbTransaction? transaction = null);
        Task<bool> deleteExercise(Guid exerciseId, IDbTransaction? transaction = null);
    }
}
