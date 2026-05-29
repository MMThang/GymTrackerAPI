using GymTracker.DTOs.SetDTOs;
using System.Data;

namespace GymTracker.Interfaces
{
    public interface ISet
    {
        Task<bool> createSet(Guid exerciseId, SetDTO setDTO, IDbTransaction? transaction = null);
        Task<bool> updateSet(Guid setId, SetDTO setDTO, IDbTransaction? transaction = null);
        Task<bool> deleteSet(Guid setId, IDbTransaction? transaction = null);
    }
}
