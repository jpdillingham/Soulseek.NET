import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';

import Response from './Response';

import { 
    Segment, 
    Input, 
    Loader,
    Button
} from 'semantic-ui-react';

const initialState = { 
    searchPhrase: '', 
    searchState: 'complete', 
    searchStatus: { 
        responseCount: 0, 
        fileCount: 0 
    }, 
    results: [], 
    interval: undefined,
    displayCount: 5,
};

class Search extends Component {
    state = initialState;

    search = () => {
        let searchPhrase = this.inputtext.inputRef.current.value;

        this.setState({ searchPhrase: searchPhrase, searchState: 'pending' }, () => {
            localStorage.setItem('searchPhrase', searchPhrase);

            axios.post(BASE_URL + '/searches', JSON.stringify({ searchText: searchPhrase }), { 
                headers: {'Content-Type': 'application/json; charset=utf-8'} 
            })
            .then(response => this.setState({ results: response.data }, () => localStorage.setItem('results', JSON.stringify(response.data))))
            .then(() => this.setState({ searchState: 'complete' }))
        });
    }

    clear = () => {
        this.setState(initialState, () => {
            localStorage.removeItem('results');
            localStorage.removeItem('searchPhrase');
        });
    }

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }

    componentDidMount = () => {
        this.fetch();
        this.setState({ 
            interval: window.setInterval(this.fetch, 500), 
            results: JSON.parse(localStorage.getItem('results')) || [],
            searchPhrase: localStorage.getItem('searchPhrase')
        }, () => {
            this.inputtext.inputRef.current.value = this.state.searchPhrase;
            this.inputtext.inputRef.current.disabled = true;
        });
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetch = () => {
        if (this.state.searchState === 'pending') {
            axios.get(BASE_URL + '/searches/' + encodeURI(this.state.searchPhrase))
            .then(response => this.setState({
                searchStatus: response.data
            }));
        }
    }

    showMore = () => {
        this.setState({ displayCount: this.state.displayCount + 5 });
    }
    
    render = () => {
        let { searchState, searchStatus, results, displayCount } = this.state;
        let pending = searchState === 'pending';

        const remainingCount = results.length - displayCount;
        const showMoreCount = remainingCount >= 5 ? 5 : remainingCount;

        return (
            <div>
                <Segment className='search-segment' raised>
                    <Input 
                        size='big'
                        ref={input => this.inputtext = input}
                        loading={pending}
                        disabled={pending}
                        className='search-input'
                        placeholder="Enter search phrase..."
                        action={!pending && { content: 'Search', onClick: this.search}}
                    />
                </Segment>
                {pending ? 
                    <Loader 
                        className='search-loader'
                        active 
                        inline='centered' 
                        size='big'
                    >
                        Found {searchStatus.fileCount} files from {searchStatus.responseCount} users
                    </Loader>
                : 
                    <div>
                        {results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).slice(0, displayCount).map((r, i) =>
                            <Response 
                                key={i} 
                                response={r} 
                                onDownload={this.props.onDownload}
                            />
                        )}
                        {remainingCount > 0 ? 
                            <Button 
                                className='showmore-button' 
                                size='large' 
                                fluid 
                                primary 
                                onClick={() => this.showMore()}>
                                    Show {showMoreCount} More Results {remainingCount > 5 ? `(${remainingCount} more hidden)` : ''}
                            </Button> 
                            : ''}
                    </div>}
                <div>&nbsp;</div>
            </div>
        )
    }
}

export default Search;