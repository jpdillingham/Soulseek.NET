import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';
import data from './data';

import Response from './Response';

import { 
    Segment, 
    Input 
} from 'semantic-ui-react';

class Search extends Component {
    state = { searchPhrase: '', searchState: 'complete', results: data }

    search = () => {
        let searchPhrase = this.inputtext.inputRef.current.value;

        this.setState({ searchState: 'pending' }, () => {
            axios.post(BASE_URL + '/search', JSON.stringify(searchPhrase), { 
                headers: {'Content-Type': 'application/json; charset=utf-8'} 
            })
            .then(response => this.setState({ results: response.data }))
            .then(() => this.setState({ searchState: 'complete' }))
        });
    }

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }
    
    render = () => {
        let { searchState, results } = this.state;
        let pending = searchState === 'pending';

        return (
            <div>
                <Segment className='search-segment'>
                    <Input 
                        ref={input => this.inputtext = input}
                        loading={pending}
                        disabled={pending}
                        className='search-input'
                        placeholder="Enter search phrase..."
                        action={!pending && { content: 'Search', onClick: this.search}}
                    />
                </Segment>
                {searchState === 'complete' && <div>
                    {results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).map((r, i) =>
                        <Response 
                            key={i} 
                            response={r} 
                            onDownload={this.props.onDownload}
                        />
                    )}
                </div>}
                <div>&nbsp;</div>
            </div>
        )
    }
}

export default Search;