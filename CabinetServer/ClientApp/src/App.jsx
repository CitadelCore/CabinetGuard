import React from 'react';
import { Router, Route } from 'react-router-dom';
import { connect } from 'react-redux';

import { history } from './helpers/history';
import { alertActions } from './actions/alert.actions';

import { PrivateRoute } from './components/PrivateRoute';
import { Layout } from './components/Layout';
import Home from './components/Home';
import { Login } from './components/auth/Login';
import { SetupWizard } from './components/admin/SetupWizard';

class App extends React.Component {
    displayName = 'App';

    constructor(props) {
        super(props);

        const { dispatch } = this.props;
        history.listen((location, action) => {
            dispatch(alertActions.clear());
        });
    }

    render() {
        const { alert } = this.props;
        return (
            <div className="jumbotron">
                <div className="container">
                    <div className="col-sm-8 col-sm-offset-2">
                        {alert.message &&
                            <div className={`alert ${alert.type}`}>{alert.message}</div>}
                        <Router history={history}>
                            <div>
                                <PrivateRoute exact path="/" component={Home} />
                                <Route path="/login" component={Login} />
                                <Route path="/setup" component={SetupWizard} />
                            </div>
                        </Router>
                    </div>
                </div>
            </div>
        );
    }
}

function mapStateToProps(state) {
    const { alert } = state;
    return {
        alert
    };
}

const connectedApp = connect(mapStateToProps)(App);
export { connectedApp as App };