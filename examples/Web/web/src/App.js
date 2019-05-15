import React, { Component } from 'react';
import axios from 'axios';
import { Route, Link, Switch } from "react-router-dom";
import { getFileName, downloadFile } from './util'
import './App.css';

import { 
    Sidebar,
    Segment,
    Menu,
    Icon,
} from 'semantic-ui-react';

import Search from './Search';
import Downloads from './Downloads';

const BASE_URL = "http://localhost:5000/api/v1";

class App extends Component {
    download = (username, files) => {
        return Promise.all(files.map(f => this.downloadOne(username, f)));
    }

    downloadOne = (username, file, toBrowser = false) => {
        // return axios.request({
        //     method: 'GET',
        //     url: `${BASE_URL}/files/${username}/${encodeURI(file.filename)}`,
        //     responseType: 'arraybuffer',
        //     responseEncoding: 'binary'
        // })
        // .then((response) => { 
        //     if (toBrowser) { 
        //         downloadFile(response.data, getFileName(file.filename))
        //     }
        // });

        return axios.post(`${BASE_URL}/files/queue/${username}/${encodeURI(file.filename)}`);
    }

    render() {
        return (
            <Sidebar.Pushable as={Segment} className='app'>
                <Sidebar as={Menu} animation='overlay' icon='labeled' inverted horizontal direction='top' visible width='thin'>
                    <Link to='/'>
                        <Menu.Item>
                            <Icon name='search' />
                            Search
                        </Menu.Item>
                    </Link>
                    <Link to='/downloads'>
                        <Menu.Item>
                            <Icon name='download' />
                            Downloads
                        </Menu.Item>
                    </Link>
                </Sidebar>
                <Sidebar.Pusher className='app-content'>
                    <Switch>
                        <Route exact path='/' component={() => <Search baseUrl={BASE_URL} onDownload={this.download}/>}/>
                        <Route path='/downloads/' component={() => <Downloads/>}/>
                    </Switch>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        )
    }
}

export default App;
