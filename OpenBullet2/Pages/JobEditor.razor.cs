﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using OpenBullet2.Auth;
using OpenBullet2.Core.Entities;
using OpenBullet2.Core.Models.Jobs;
using OpenBullet2.Core.Repositories;
using OpenBullet2.Core.Services;
using OpenBullet2.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBullet2.Pages;

public partial class JobEditor
{
    private JobEntity jobEntity;
    private JobOptions jobOptions;
    private readonly JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.Auto };
    private int uid = -1;
    [Parameter] public int JobId { get; set; }

    [Inject] private IJobRepository JobRepo { get; set; }
    [Inject] private JobManagerService Manager { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    [Inject] private JobFactoryService JobFactory { get; set; }
    [Inject] private AuthenticationStateProvider Auth { get; set; }

    protected async override Task OnInitializedAsync()
    {
        uid = await ((OBAuthenticationStateProvider)Auth).GetCurrentUserId();
        jobEntity = await JobRepo.Get(JobId);
        jobOptions = JsonConvert.DeserializeObject<JobOptionsWrapper>(jobEntity.JobOptions, settings).Options;
    }

    private bool CanSeeJob(int ownerId)
        => uid == 0 || ownerId == uid;

    private async Task Edit()
    {
        var wrapper = new JobOptionsWrapper { Options = jobOptions };
        jobEntity.JobOptions = JsonConvert.SerializeObject(wrapper, settings);

        await JobRepo.Update(jobEntity);

        try
        {
            var oldJob = Manager.Jobs.First(j => j.Id == JobId);
            var newJob = JobFactory.FromOptions(JobId, jobEntity.Owner == null ? 0 : jobEntity.Owner.Id, jobOptions);

            Manager.RemoveJob(oldJob);
            Manager.AddJob(newJob);
            Nav.NavigateTo($"job/{JobId}");
        }
        catch (Exception ex)
        {
            await js.AlertException(ex);
        }
    }
}