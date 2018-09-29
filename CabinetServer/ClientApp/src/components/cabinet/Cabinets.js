import React from 'react';
import { Link } from 'react-router-dom';
import { connect } from 'react-redux';

export class Cabinets extends Component {
  displayName = Cabinets.name

  constructor() {
    super();
    this.state = { cabinets: [], loading: true };

    fetch('api/Cabinets')
        .then(response => response.json())
        .then(data => {
            this.setState({ cabinets: data, loading: false });
        });
  }

  static renderCabinetsTable(cabinets) {
      return (
          <table className='table'>
              <thead>
                  <tr>
                      <th>Name</th>
                      <th>Controller</th>
                      <th>Armed</th>
                      <th>Security alarm</th>
                      <th>Fire alarm</th>
                      <th>Override</th>
                  </tr>
              </thead>
              <tbody>
                  {cabinets.map(cabinet =>
                      <tr key={cabinet.Id}>
                          <td>{cabinet.Nickname}</td>
                          <td>{cabinet.ControllerId}</td>
                          <td>{cabinet.SecurityAlerted}</td>
                          <td>{cabinet.FireAlerted}</td>
                          <td>{cabinet.Override}</td>
                      </tr>
                  )}
              </tbody>
          </table>
      );
  }

  incrementCounter() {
    this.setState({
      currentCount: this.state.currentCount + 1
    });
  }

  render() {
      let contents = this.state.loading
          ? <p><em>Please wait...</em></p>
          : Cabinets.renderCabinetsTable(this.state.cabinets);

      return (
          <div>
              <h1>Cabinets in your company</h1>
              <p>Displaying an administrative overview of cabinets in your organization.</p>
              {contents}
          </div>
      );
  }
}
