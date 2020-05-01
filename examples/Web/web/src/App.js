import React, { Component } from 'react';
import { Route, Link, Switch } from "react-router-dom";
import { tokenKey, tokenPassthroughValue } from './config';
import api from './api';

import './App.css';
import Search from './Search';
import Browse from './Browse/Browse';
import Transfers from './Transfers';
import LoginForm from './LoginForm';

import { 
    Sidebar,
    Segment,
    Menu,
    Icon,
    Modal,
    Header
} from 'semantic-ui-react';

const initialState = {
    token: undefined,
    login: {
        initialized: false,
        pending: false,
        error: undefined
    }
};

class App extends Component {
    state = initialState;

    componentDidMount = async () => {
        const { login } = this.state;

        const response = await api.get('/session/enabled');
        const securityEnabled = response.data;

        if (securityEnabled) {
            this.loadToken();
        } else {
            this.setToken(localStorage, tokenPassthroughValue)
        }

        this.setState({ login: { ...login, initialized: true } })
    }

    loadToken = () => {
        const token = JSON.parse(sessionStorage.getItem(tokenKey) || localStorage.getItem(tokenKey));
        this.setState({ token });
    }

    setToken = (storage, token) => {
        console.log(storage, token);
        this.setState({ token }, () => storage.setItem(tokenKey, JSON.stringify(token)));
    }

    removeToken = (storage) => {
        this.setState({ token: undefined }, () => storage.removeItem(tokenKey));
    }

    login = (username, password, rememberMe) => {
        this.setState({ login: { ...this.state.login, pending: true, error: undefined }}, async () => {
            try {
                const response = await api.post('/session', { username, password });
                this.setToken(rememberMe ? localStorage : sessionStorage, response.data.token);
            } catch (error) {
                this.setState({ login: { pending: false, error }});
            }
        });
    }
    
    logout = () => {
        localStorage.removeItem(tokenKey);
        this.setState({ ...initialState, login: { ...initialState.login, initialized: true }});
    }

    render = () => {
        const { token, login } = this.state;

        return (
            <>
            {!token ? 
            <LoginForm 
                onLoginAttempt={this.login} 
                initialized={login.initialized}
                loading={login.pending} 
                error={login.error}
            /> : 
            <Sidebar.Pushable as={Segment} className='app'>
                <Sidebar 
                    as={Menu} 
                    animation='overlay' 
                    icon='labeled' 
                    inverted 
                    horizontal='true'
                    direction='top' 
                    visible width='thin'
                >
                    <Link to='/'>
                        <Menu.Item>
                            <Icon name='search'/>Search
                        </Menu.Item>
                    </Link>
                    <Link to='/browse'>
                        <Menu.Item>
                            <Icon name='folder open'/>Browse
                        </Menu.Item>
                    </Link>
                    <Link to='/downloads'>
                        <Menu.Item>
                            <Icon name='download'/>Downloads
                        </Menu.Item>
                    </Link>
                    <Link to='/uploads'>
                        <Menu.Item>
                            <Icon name='upload'/>Uploads
                        </Menu.Item>
                    </Link>
                    {token !== tokenPassthroughValue && <Modal
                        trigger={
                            <Menu.Item position='right'>
                                <Icon name='sign-out'/>Log Out
                            </Menu.Item>
                        }
                        centered
                        size='mini'
                        header={<Header icon='sign-out' content='Confirm Log Out' />}
                        content='Are you sure you want to log out?'
                        actions={['Cancel', { key: 'done', content: 'Log Out', negative: true, onClick: this.logout }]}
                    />}
                </Sidebar>
                <Sidebar.Pusher className='app-content'>
                    <Switch>
                        <Route exact path='/' component={Search}/>
                        <Route path='/browse/' component={Browse}/>
                        <Route path='/downloads/' render={(props) => <Transfers {...props} direction='download'/>}/>
                        <Route path='/uploads/' render={(props) => <Transfers {...props} direction='upload'/>}/>
                    </Switch>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
            }</>
        )
    }
}

export default App;
