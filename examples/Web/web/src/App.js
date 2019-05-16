import React, { Component } from 'react';
import { Route, Link, Switch } from "react-router-dom";

import './App.css';
import Search from './Search';
import Downloads from './Downloads';

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
                    <Link to='/downloads'>
                        <Menu.Item>
                            <Icon name='download'/>Downloads
                        </Menu.Item>
                    </Link>
                </Sidebar>
                <Sidebar.Pusher className='app-content'>
                    <Switch>
                        <Route exact path='/' component={Search}/>
                        <Route path='/downloads/' component={Downloads}/>
                    </Switch>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        )
    }
}

export default App;
