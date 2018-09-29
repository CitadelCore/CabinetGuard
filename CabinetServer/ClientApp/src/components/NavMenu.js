import React from 'react';
import { Link } from 'react-router-dom';
import { Glyphicon, Nav, Navbar, NavItem } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import fontawesome from '@fortawesome/fontawesome';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faHome, faWarehouse, faServer, faList, faCog } from '@fortawesome/fontawesome-free-solid';
import './NavMenu.css';

export default props => (
  <Navbar inverse fixedTop fluid collapseOnSelect>
    <Navbar.Header>
      <Navbar.Brand>
        <Link to={'/'}>CCS 2018</Link>
      </Navbar.Brand>
      <Navbar.Toggle />
    </Navbar.Header>
    <Navbar.Collapse>
      <Nav>
        <LinkContainer to={'/'} exact>
              <NavItem>
                <FontAwesomeIcon icon='home' /> Dashboard
              </NavItem>
            </LinkContainer>
            <LinkContainer to={'/cabinets'}>
              <NavItem>
                <FontAwesomeIcon icon='warehouse' /> Cabinets
              </NavItem>
            </LinkContainer>
            <LinkContainer to={'/servers'}>
              <NavItem>
                <FontAwesomeIcon icon='server' /> Servers
              </NavItem>
            </LinkContainer>
            <LinkContainer to={'/commands'}>
              <NavItem>
                <FontAwesomeIcon icon='list' /> Commands
              </NavItem>
            </LinkContainer>
            <LinkContainer to={'/admin'}>
              <NavItem>
                <FontAwesomeIcon icon='cog' /> Administration
              </NavItem>
            </LinkContainer>
      </Nav>
    </Navbar.Collapse>
  </Navbar>
);
