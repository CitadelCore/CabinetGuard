import { authHeader } from '../helpers/auth-header';
import { handleResponse, handleError } from '../helpers/request.helper';

export const adminService = {
    setup
};

/**
 * Attempts to set up the appliance with default admin credentials.
 * @param {any} setupData
 */
function setup(setupData) {
    const requestOptions = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ setupData })
    };

    return fetch('/api/Admin/Setup', requestOptions)
        .then(handleResponse, handleError);
}