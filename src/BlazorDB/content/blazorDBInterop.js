window.blazorDBInterop = {
    setItem: function(key, value, session) {
        if (session) {
            sessionStorage.setItem(key, value);
        } else {
            localStorage.setItem(key, value);
        }
        return true;
    },
    getItem: function (key, session) {
        if(session) {
            return sessionStorage.getItem(key);
        } else {
            return localStorage.getItem(key);
        }
    },
    removeItem: function (key, session) {
        if(session) {
            sessionStorage.removeItem(key);
        } else {
            localStorage.removeItem(key);
        }
        return true;
    },
    clear: function (session) {
        if(session) {
            sessionStorage.clear();
        } else {
            localStorage.clear();
        }
        return true;
    }
};