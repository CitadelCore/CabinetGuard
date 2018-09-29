export function handleResponse(response) {
    return new Promise((resolve, reject) => {
        var contentType = response.headers.get("Content-Type");
        if (response.ok) {
            // Return JSON only if it was returned
            if (contentType && contentType.includes("application/json")) {
                response.JSON().then(json => resolve(json));
            } else {
                resolve();
            }
        } else {
            // Response was not OK. Return the error.
            if (contentType && contentType.includes("application/json")) {
                response.text().then(text => reject(JSON.parse(text).error));
            } else {
                response.text().then(text => reject(text));
            }
        }
    });
}

export function handleError(error) {
    return Promise.reject(error && error.message);
}