﻿namespace XYVR.Core;

public class InMemoryCredentialsStorage : ICredentialsStorage
{
    private string? _token;

    public InMemoryCredentialsStorage(string? token)
    {
        _token = token;
    }
    
    public async Task<string?> RequireCookieOrToken()
    {
        await Task.CompletedTask;
        return _token;
    }

    public async Task StoreCookieOrToken(string cookie__sensitive)
    {
        await Task.CompletedTask;
        _token = cookie__sensitive;
    }

    public async Task DeleteCookieOrToken()
    {
        await Task.CompletedTask;
        _token = null;
    }
}