import React from 'react';
import Link from 'react-router-dom';
import { connect } from 'react-redux';

import { userActions } from '../../actions/user.actions';

class Login extends React.Component {
    constructor(props) {
        super(props);

        // Log out existing user sessions
        this.props.dispatch(userActions.logout());

        // Clear the state
        this.state = {
            username: '',
            password: '',
            submitted: false
        };

        this.handleChange = this.handleChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
    }

    handleChange(e) {
        const { name, value } = e.target;
        this.setState({ [name]: value });
    }

    handleSubmit(e) {
        e.preventDefault();

        this.setState({ submitted: true });
        const { username, password } = this.state;
        const { dispatch } = this.props;
        if (username && password) {
            dispatch(userActions.login(username, password));
        }
    }

    render() {
        const { loggingIn } = this.props;
        const { username, password, submitted } = this.state;
        return (
            <div className="col-md-6 col-md-offset-3">
                <h2>Hello.</h2>
                <form name="form" onSubmit={this.handleSubmit}>
                    <div className={'form-group' + (submitted && !username ? ' has-error' : '')}>
                        <label htmlFor="username">Username</label>
                        <input type="text" className="form-control" name="username" value={username} onChange={this.handleChange} />
                        {submitted && !username &&
                            <div className="help-block">Username cannot be blank</div>
                        }
                    </div>
                    <div className={'form-group' + (submitted && !username ? ' has-error' : '')}>
                        <label htmlFor="password">Password</label>
                        <input type="password" className="form-control" name="password" value={password} onChange={this.handleChange} />
                        {submitted && !password &&
                            <div className="help-block">Password cannot be blank</div>
                        }
                    </div>
                    <div className="form-group">
                        <button className="btn btn-primary">Log in</button>
                    </div>
                    This is a restricted area. You may be committing an offense by attempting to gain access to this service if you are not authorized.
                </form>
            </div>
            );
    }
}

function mapStateToProps(state) {
    const { loggingIn } = state.auth;
    return {
        loggingIn
    };
}

const connectedLoginPage = connect(mapStateToProps)(Login);
export { connectedLoginPage as Login };