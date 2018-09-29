import { authHeader } from '../helpers/auth-header';
import { handleResponse, handleError } from '../helpers/request.helper';

export const userService = {
    login,
    logout,
    get,
    getAll,
    update,
    remove
};

// Logs in a user with the specified username and password.
function login(username, password) {
    const requestOptions = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
    };

    return fetch('/api/Users/Authenticate', requestOptions)
        .then(handleResponse, handleError)
        .then(user => {
            if (user && user.token) {
                localStorage.setItem('user', JSON.stringify(user));
            }
        });
}

// Logs out a user by removing the object from the local cache.
function logout() {
    localStorage.removeItem('user');
}

// Retrieves a user from the server.
function get(username) {
    const requestOptions = {
        method: 'GET',
        headers: authHeader()
    };

    return fetch('/api/Users/' + username, requestOptions)
        .then(handleResponse, handleError);
}

// Retrieves a list of all users from the server.
// This is an administrative action.
function getAll() {
    const requestOptions = {
        method: 'GET',
        headers: authHeader()
    };

    return fetch('/api/Users', requestOptions)
        .then(handleResponse, handleError);
}

// Updates a user.
function update(user) {
    const requestOptions = {
        method: 'PUT',
        headers: { ...authHeader(), 'Content-Type': 'application/json' },
        body: JSON.stringify(user)
    };

    return fetch('/api/Users/' + user.username, requestOptions)
        .then(handleResponse, handleError);
}

// Removes a user. (rarely used)
function remove(username) {
    const requestOptions = {
        method: 'DELETE',
        headers: authHeader()
    };

    return fetch('/api/Users/' + username, requestOptions)
        .then(handleResponse, handleError);
}