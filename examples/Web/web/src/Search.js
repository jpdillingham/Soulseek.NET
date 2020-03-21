import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';

import Response from './Response';

import { 
    Segment, 
    Input, 
    Loader,
    Button,
    Dropdown,
    Checkbox
} from 'semantic-ui-react';

const initialState = { 
    searchPhrase: '', 
    searchState: 'idle', 
    searchStatus: { 
        responseCount: 0, 
        fileCount: 0 
    }, 
    results: [], 
    interval: undefined,
    displayCount: 5,
    resultSort: 'uploadSpeed',
    hideNoFreeSlots: true
};

const sortOptions = {
    uploadSpeed: { field: 'uploadSpeed', order: 'desc' },
    queueLength: { field: 'queueLength', order: 'asc' }
}

const sortDropdownOptions = [
    { key: 'uploadSpeed', text: 'Upload Speed (Fastest to Slowest)', value: 'uploadSpeed' },
    { key: 'queueLength', text: 'Queue Depth (Least to Most)', value: 'queueLength' }
];

class Search extends Component {
    state = initialState;

    search = () => {
        let searchPhrase = this.inputtext.inputRef.current.value;

        this.setState({ searchPhrase: searchPhrase, searchState: 'pending' }, () => {
            axios.post(BASE_URL + '/searches', JSON.stringify({ searchText: searchPhrase }), { 
                headers: {'Content-Type': 'application/json; charset=utf-8'} 
            })
            .then(response => this.setState({ results: response.data }))
            .then(() => this.setState({ searchState: 'complete' }, () => {
                this.saveState();
                this.setSearchText();
            }))
        });
    }

    clear = () => {
        this.setState(initialState, () => {
            this.saveState();
            this.setSearchText();
        });
    }

    onSearchPhraseChange = (event, data) => {
        this.setState({ searchPhrase: data.value });
    }

    saveState = () => {
        localStorage.setItem('soulseek-example-search-state', JSON.stringify(this.state));
    }

    loadState = () => {
        this.setState(JSON.parse(localStorage.getItem('soulseek-example-search-state')) || initialState);
    }

    componentDidMount = () => {
        this.fetchStatus();
        this.loadState();
        this.setState({ 
            interval: window.setInterval(this.fetchStatus, 500)
        }, () => this.setSearchText());
    }

    setSearchText = () => {
        this.inputtext.inputRef.current.value = this.state.searchPhrase;
        this.inputtext.inputRef.current.disabled = this.state.searchState !== 'idle';
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetchStatus = () => {
        if (this.state.searchState === 'pending') {
            axios.get(BASE_URL + '/searches/' + encodeURI(this.state.searchPhrase))
            .then(response => this.setState({
                searchStatus: response.data
            }));
        }
    }

    showMore = () => {
        this.setState({ displayCount: this.state.displayCount + 5 }, () => this.saveState());
    }

    sortAndFilterResults = (results) => {
        const { hideNoFreeSlots, resultSort } = this.state;
        const { field, order } = sortOptions[resultSort];

        return results.filter(r => !(hideNoFreeSlots && r.freeUploadSlots === 0)).sort((a, b) => {
            if (order === 'asc') {
                return a[field] - b[field];
            }

            return b[field] - a[field];
        });
    }
    
    render = () => {
        let { searchState, searchStatus, results, displayCount, resultSort, hideNoFreeSlots } = this.state;
        let pending = searchState === 'pending';

        const sortedAndFilteredResults = this.sortAndFilterResults(results);

        const remainingCount = sortedAndFilteredResults.length - displayCount;
        const showMoreCount = remainingCount >= 5 ? 5 : remainingCount;
        const hiddenCount = results.length - sortedAndFilteredResults.length;

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
                        action={!pending && (searchState === 'idle' ? { content: 'Search', onClick: this.search } : { content: 'Clear Results', color: 'red', onClick: this.clear })} 
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
                        {sortedAndFilteredResults && sortedAndFilteredResults.length > 0 && <Segment className='search-options' raised>
                            <Dropdown
                                button
                                className='icon'
                                floating
                                labeled
                                icon='sort'
                                options={sortDropdownOptions}
                                onChange={(e, { value }) => this.setState({ resultSort: value }, () => this.saveState())}
                                text={sortDropdownOptions.find(o => o.value === resultSort).text}
                            />
                            <Checkbox
                                className='search-options-hide-no-slots'
                                toggle
                                onChange={() => this.setState({ hideNoFreeSlots: !hideNoFreeSlots }, () => this.saveState())}
                                checked={hideNoFreeSlots}
                                label='Hide Results with No Free Slots' 
                            />
                        </Segment>}
                        {sortedAndFilteredResults.slice(0, displayCount).map((r, i) =>
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
                                    Show {showMoreCount} More Results {remainingCount > 5 ? `(${remainingCount} remaining, ${hiddenCount} hidden by filter(s))` : ''}
                            </Button> 
                            : ''}
                    </div>}
                <div>&nbsp;</div>
            </div>
        )
    }
}

export default Search;