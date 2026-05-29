using GymTracker.DTOs.WorkoutSessionDTOs;

namespace GymTracker.Interfaces
{
    public interface IWorkoutSession
    {
        Task<bool> createWorkoutSession(CreateWorkoutSessionDTO workoutSession);
        Task<List<WorkoutSessionSummaryDTO>> getWorkoutSessionsList(Guid userId);
        Task<Guid?> getWorkoutSessionUserId(Guid workoutSessionId);
        Task<WorkoutSessionDetailDTO> getWorkoutSession(Guid workoutSessionId);
        Task<List<WorkoutSessionCalendarDTO>> getWorkoutSessionsByMonthYear(Guid userId, int month, int year);
        Task<bool> updateWorkoutSession(UpdateWorkoutSessionDTO updateDTO);
        Task<bool> deleteWorkoutSession(Guid workoutSessionId);
    }
}
