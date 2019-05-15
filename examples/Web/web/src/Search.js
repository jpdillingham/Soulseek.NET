import React, { Component } from 'react';
import axios from 'axios';

import Response from './Response';
import SearchBox from './SearchBox';

import data from './data'

class Search extends Component {
    state = { searchPhrase: '', searchState: 'complete', results: data }

    search = () => {
        this.setState({ searchState: 'pending' }, () => {
            axios.get(this.props.baseUrl + '/search/' + this.state.searchPhrase)
            .then(response => this.setState({ results: response.data }))
            .then(() => this.setState({ searchState: 'complete' }))
        });
    }

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }
    
    render = () => (
        <div>
            <SearchBox
                pending={this.state.searchState === 'pending'}
                onPhraseChange={this.onSearchPhraseChange}
                onSearch={this.search}
            />
            {this.state.searchState === 'complete' && <div>
                {this.state.results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).map(r =>
                    <Response response={r} onDownload={this.props.onDownload}/>
                )}
            </div>}
        </div>
    )
}

export default Search;