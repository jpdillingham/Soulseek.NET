import React, { Component } from 'react';
import { Route, Link, Switch } from "react-router-dom";

import './App.css';
import Search from './Search';
import Browse from './Browse';
import Transfers from './Transfers';

import { 
    Sidebar,
    Segment,
    Menu,
    Icon,
} from 'semantic-ui-react';

class App extends Component {
    render = () => {
        return (
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
        )
    }
}

export default App;
