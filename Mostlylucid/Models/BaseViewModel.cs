﻿namespace Mostlylucid.Models;

public class BaseViewModel
{
    
    public bool Authenticated { get; set; }
    public string? Name { get; set; }
    
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    
    public bool IsAdmin { get; set; }
}