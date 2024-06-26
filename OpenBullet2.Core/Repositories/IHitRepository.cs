﻿using OpenBullet2.Core.Entities;

namespace OpenBullet2.Core.Repositories;

/// <summary>
///     Stores hits.
/// </summary>
public interface IHitRepository : IRepository<HitEntity>
{
    /// <summary>
    ///     Deletes all hits from the repository.
    /// </summary>
    void Purge();
}