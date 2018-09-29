import { adminConstants } from '../constants/admin.constants';

const initialState = { settingUp: false, setupFinished: false };

export function setup(state = initialState, action) {
    switch (action.type) {
        case adminConstants.SETUP_REQUEST:
            return {
                settingUp: true
            };
        case adminConstants.SETUP_SUCCESS:
            return {
                setupFinished: true
            };
        case adminConstants.SETUP_FAILURE:
            return {};
        default:
            return state;
    }
}