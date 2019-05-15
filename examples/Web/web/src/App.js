import React, { Component } from 'react';
import axios from 'axios';
import { Route, Link } from "react-router-dom";
import { getFileName, downloadFile } from './util'
import './App.css';

import { 
    Sidebar,
    Segment,
    Menu,
    Icon,
} from 'semantic-ui-react';

import Response from './Response';
import Search from './Search'

import data from './data'

const BASE_URL = "http://localhost:5000/api/v1";

class App extends Component {
    state = { searchPhrase: '', searchState: 'complete', results: data }

    search = () => {
        this.setState({ searchState: 'pending' }, () => {
            axios.get(BASE_URL + '/search/' + this.state.searchPhrase)
            .then(response => this.setState({ results: response.data }))
            .then(() => this.setState({ searchState: 'complete' }))
        });
    }

    download = (username, files) => {
        Promise.all(files.map(f => this.downloadOne(username, f)));
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

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }

    render() {
        return (
            <Sidebar.Pushable as={Segment} className='app'>
                <Sidebar as={Menu} animation='overlay' icon='labeled' inverted horizontal direction='top' visible width='thin'>
                    <Link to='/search'>
                        <Menu.Item as='a'>
                            <Icon name='search' />
                            Search
                        </Menu.Item>
                    </Link>
                    <Link to='/downloads'>
                        <Menu.Item as='a'>
                            <Icon name='download' />
                            Downloads
                        </Menu.Item>
                    </Link>
                </Sidebar>
                <Sidebar.Pusher className='content'>
                    <Search
                        pending={this.state.searchState === 'pending'}
                        onPhraseChange={this.onSearchPhraseChange}
                        onSearch={this.search}
                    />
                    {this.state.searchState === 'complete' && <div>
                        {this.state.results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).map(r =>
                            <Response response={r} onDownload={this.download}/>
                        )}
                    </div>}
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        )
    }
}

export default App;
