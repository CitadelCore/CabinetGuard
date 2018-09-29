import { userConstants } from '../constants/user.constants';
import { userService } from '../services/user.service';
import { alertActions } from './alert.actions';
import { history } from '../helpers/history';

export const userActions = {
login,
logout,
remove,
};

function login(username, password) {
    return dispatch => {
        dispatch(request({ username }));
        userService.login(username, password)
            .then(
                user => {
                    dispatch(success(user));
                    history.push('/');
                },
                error => {
                    dispatch(failure(error));
                    dispatch(alertActions.error(error));
                }
            );
    };

    function request(user) { return { type: userConstants.LOGIN_REQUEST, user }; }
    function success(user) { return { type: userConstants.LOGIN_SUCCESS, user }; }
    function failure(error) { return { type: userConstants.LOGIN_FAILURE, error }; }
}

function logout() {
    userService.logout();
    return { type: userConstants.LOGOUT };
}

function remove(user) {
    return dispatch => {
        dispatch(request(user));
        userService.delete(user)
            .then(
                user => {
                    dispatch(success(user));
                },
                error => {
                    dispatch(failure(user, error));
                },
        );
    };

    function request(user) { return { type: userConstants.REMOVE_REQUEST, user }; }
    function success(user) { return { type: userConstants.REMOVE_SUCCESS, user }; }
    function failure(user, error) { return { type: userConstants.REMOVE_FAILURE, user, error }; }
}

