import { adminConstants } from '../constants/admin.constants';
import { adminService } from '../services/admin.service';
import { alertActions } from './alert.actions';
import { history } from '../helpers/history';

export const adminActions = {
    setup,
};

function setup(setupData) {
    return dispatch => {
        dispatch(request({ setupData }));
        adminService.setup(setupData)
            .then(
                setupData => {
                    dispatch(success(setupData));
                    history.push('/login');
                },
                error => {
                    dispatch(failure(error));
                    dispatch(alertActions.error(error));
                }
            );
    };

    function request(setupData) { return { type: adminConstants.SETUP_REQUEST, setupData }; }
    function success(setupData) { return { type: adminConstants.SETUP_SUCCESS, setupData }; }
    function failure(error) { return { type: adminConstants.SETUP_FAILURE, error }; }
}