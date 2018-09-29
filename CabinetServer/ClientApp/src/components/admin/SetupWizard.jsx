import React from 'react';
import Link from 'react-router-dom';
import { connect } from 'react-redux';

import { adminActions } from '../../actions/admin.actions';

/**
 * Provides a page where users can run initial setup of the appliance.
 * */
class SetupWizard extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
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
        const { password } = this.state;
        const { dispatch } = this.props;

        
        if (password) {
            const setupData =
                {
                    User: {
                        Username: "admin",
                        Password: password
                    }
                }

            dispatch(adminActions.setup(setupData));
        }
    }

    render() {
        const { settingUp } = this.props;
        const { password, submitted } = this.state;
        return (
            <div className="col-md-6 col-md-offset-3">
                <h2>Initial setup</h2>
                <form name="form" onSubmit={this.handleSubmit}>
                        <label htmlFor="username">Username</label>
                        <input disabled type="text" className="form-control" name="username" value="admin" />
                    <div className={'form-group' + (submitted && !password ? ' has-error' : '')}>
                        <label htmlFor="password">Password</label>
                        <input type="password" className="form-control" name="password" value={password} onChange={this.handleChange} />
                        {submitted && !password &&
                            <div className="help-block">Password cannot be blank</div>
                        }
                    </div>
                    <div className="form-group">
                        <button className="btn btn-primary">Complete setup</button>
                    </div>
                </form>
            </div>
        );
    }
}

function mapStateToProps(state) {
    const { settingUp } = state.setup;
    return {
        settingUp
    };
}

const connectedSetupPage = connect(mapStateToProps)(SetupWizard);
export { connectedSetupPage as SetupWizard };