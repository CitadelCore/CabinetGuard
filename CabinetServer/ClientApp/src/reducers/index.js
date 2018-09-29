import { combineReducers } from 'redux';
import { setup } from './admin.reducer';
import { alert } from './alert.reducer';
import { auth } from './auth.reducer';
import { users } from './users.reducer';
import { routerReducer } from 'react-router-redux';

const rootReducer = combineReducers({
    routing: routerReducer,
    setup,
    alert,
    auth,
    users
});

export default rootReducer;