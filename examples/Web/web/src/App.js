import React, { Component } from 'react';
import axios from 'axios';
import './App.css';

import Response from './Response';
import Search from './Search'

import data from './data'

const BASE_URL = "http://localhost:60084/api/v1";

class App extends Component {
    state = { searchPhrase: '', searchState: 'complete', results: data }

    search = () => {
        this.setState({ searchState: 'pending' }, () => {
            axios.get(BASE_URL + '/search/' + this.state.searchPhrase)
            .then(response => this.setState({ results: response.data }))
            .then(() => this.setState({ searchState: 'complete' }))
        })

        console.log('sadfdsa')
        //this.setState({ results: data })
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
                        <Response response={r}/>
                    )}
                </div>}
            </div>
        )
    }
}

export default App;
