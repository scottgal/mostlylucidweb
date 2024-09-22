﻿

using System.ComponentModel.DataAnnotations;

namespace Mostlylucid.Shared;

public enum SubscriptionType
{
    [Display(Name = "Daily")]
    Daily,
    [Display(Name = "Weekly")]
    Weekly,
    [Display(Name = "Every Post")]
    EveryPost,
    [Display(Name = "Monthly")]
    Monthly
}