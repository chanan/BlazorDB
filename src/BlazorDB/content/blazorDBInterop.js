Blazor.registerFunction('BlazorDB.blazorDBInterop.SetItem', function (key, value, session) {
    if (session) {
        sessionStorage.setItem(key, value);
    } else {
        localStorage.setItem(key, value);
    }
    return true;
});

Blazor.registerFunction('BlazorDB.blazorDBInterop.GetItem', function (key, session) {
    if (session) {
        return sessionStorage.getItem(key);
    } else {
        return localStorage.getItem(key);
    }
});

Blazor.registerFunction('BlazorDB.blazorDBInterop.RemoveItem', function (key, session) {
    if (session) {
        sessionStorage.removeItem(key);
    } else {
        localStorage.removeItem(key);
    }
    return true;
});


Blazor.registerFunction('BlazorDB.blazorDBInterop.Clear', function (session) {
    if (session) {
        sessionStorage.clear();
    } else {
        localStorage.clear();
    }
    return true;
});

