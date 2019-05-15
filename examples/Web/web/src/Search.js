import React, { Component } from 'react';
import axios from 'axios';

import data from './data'

import Response from './Response';

import { 
    Segment, 
    Input 
} from 'semantic-ui-react';

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
    
    render = () => {
        let pending = this.state.searchState === 'pending';

        return (
            <div>
                <Segment className='search-segment'>
                    <Input 
                        loading={pending}
                        disabled={pending}
                        className='search-input'
                        placeholder="Enter search phrase..."
                        onChange={this.onSearchPhraseChange}
                        action={!pending && { content: 'Search', onClick: this.search }}
                    />
                </Segment>
                {this.state.searchState === 'complete' && <div>
                    {this.state.results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).map(r =>
                        <Response response={r} onDownload={this.props.onDownload}/>
                    )}
                </div>}
                <div>&nbsp;</div>
            </div>
        )
    }
}

export default Search;