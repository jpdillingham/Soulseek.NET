import React, { Component } from 'react';
import axios from 'axios';
import { getFileName, downloadFile } from './util'
import './App.css';

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

    downloadOne = (username, file) => {
        return axios.request({
            method: 'GET',
            url: `${BASE_URL}/download/${username}/${encodeURI(file.filename)}`,
            responseType: 'arraybuffer',
            responseEncoding: 'binary'
        })
        .then((response) => downloadFile(response.data, getFileName(file.filename)));
    }

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }

    render() {
        return (
            <div className="app">
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
            </div>
        )
    }
}

export default App;
